﻿using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Protocol.Handler;

namespace YetAnotherXmppClient.UI.ViewModel
{
    public class RosterItemInfo
    {
        public string Jid { get; set; }
        public string Name { get; set; }
    }

    public class LoginCredentials
    {
        public string Jid { get; set; }
        public string Password { get; set; }
    }
    public class MainViewModel : ReactiveObject
    {
        private XmppClient xmppClient = new XmppClient();

        private static MainViewModel instance;
        public static DebugTextWriterDecorator stringWriter = new DebugTextWriterDecorator(new StringWriter(), _ => instance?.RaisePropertyChanged(nameof(LogText)));

        public Interaction<Unit, LoginCredentials> LoginInteraction { get; } = new Interaction<Unit, LoginCredentials>();
        public Interaction<string, bool> SubscriptionRequestInteraction { get; } = new Interaction<string, bool>();
        public Interaction<Unit, RosterItemInfo> AddRosterItemInteraction { get; } = new Interaction<Unit, RosterItemInfo>();

        public string LogText => stringWriter.Decoratee.ToString();

        public ReactiveCommand LoginCommand { get; }
        public ReactiveCommand LogoutCommand { get; }
        public ReactiveCommand StartChatCommand { get; }
        public ReactiveCommand AddRosterItemCommand { get; }
        public ReactiveCommand DeleteRosterItemCommand { get; }

        private bool isConnected;
        public bool IsConnected
        {
            get => this.isConnected;
            set => this.RaiseAndSetIfChanged(ref this.isConnected, value);
        }

        private bool isProtocolNegotiationComplete;
        public bool IsProtocolNegotiationComplete
        {
            get => this.isProtocolNegotiationComplete;
            set => this.RaiseAndSetIfChanged(ref this.isProtocolNegotiationComplete, value);
        }

        private string connectedJid;

        public string ConnectedJid
        {
            get => this.connectedJid;
            set => this.RaiseAndSetIfChanged(ref this.connectedJid, value);
        }

        private IEnumerable<RosterItem> rosterItems;
        public IEnumerable<RosterItem> RosterItems
        {
            get => this.rosterItems;
            set => this.RaiseAndSetIfChanged(ref this.rosterItems, value);
        }

        public RosterItem SelectedRosterItem { get; set; }

        public ObservableCollection<ChatSessionViewModel> ChatSessions { get; } = new ObservableCollection<ChatSessionViewModel>();

        private ChatSessionViewModel selectedChatSession;
        public ChatSessionViewModel SelectedChatSession
        {
            get => this.selectedChatSession;
            set => this.RaiseAndSetIfChanged(ref this.selectedChatSession, value);
        }


        public MainViewModel()
        {
            instance = this;

            this.LoginCommand = ReactiveCommand.CreateFromTask(this.LoginAsync);//new ActionCommand(this.OnLoginCommandExecutedAsync);
            this.LoginCommand.ThrownExceptions.Subscribe(ex => PrintException("MainViewModel.LoginAsync", ex));
            this.LogoutCommand = ReactiveCommand.CreateFromTask(this.LogoutAsync);
            this.LogoutCommand.ThrownExceptions.Subscribe(ex => PrintException("MainViewModel.LogoutAsync", ex));


            //var canExecute = this.WhenAny(x => x.SelectedRosterItem, selection => selection != null); //UNDONE
            this.StartChatCommand = ReactiveCommand.Create(this.StartChatSession);
            this.StartChatCommand.ThrownExceptions.Subscribe(ex => PrintException("MainViewModel.StartChatSession", ex));
            this.AddRosterItemCommand = ReactiveCommand.CreateFromTask(this.AddRosterItemAsync);
            this.DeleteRosterItemCommand = ReactiveCommand.CreateFromTask(this.DeleteRosterItemAsync);

            this.xmppClient.Disconnected += this.HandleDisconnected;
            this.xmppClient.ProtocolNegotiationFinished += (sender, connectedJid) =>
            {
                this.ConnectedJid = connectedJid;
                this.IsProtocolNegotiationComplete = true;
            };
            this.xmppClient.RosterUpdated += (sender, items) => this.RosterItems = items;
            this.xmppClient.SubscriptionRequestReceived += this.HandleSubscriptionRequestReceivedAsync;
            this.xmppClient.MessageReceived = this.OnMessageReceived;

            async Task PrintException(string location, Exception exception)
            {
                await stringWriter.WriteAndFlushAsync(location + ": " + exception);
            }
        }

        private void HandleDisconnected(object sender, EventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.IsConnected = false;
                    this.IsProtocolNegotiationComplete = false;
                    this.ConnectedJid = null;
                    this.RosterItems = null;
                    this.ChatSessions.Clear();
                });
        }

        private void StartChatSession()
        {
            if (this.SelectedRosterItem == null)
                return;

            // is there already a session with the same jid? //UNDONE fulljid
            var viewModel = this.ChatSessions.FirstOrDefault(vm => vm.OtherJid == this.SelectedRosterItem.Jid);
            if (viewModel == null)
            {
                var chatSession = this.xmppClient.ProtocolHandler.ImProtocolHandler.StartChatSession(this.SelectedRosterItem.Jid);
                viewModel = new ChatSessionViewModel(chatSession);
                this.ChatSessions.Add(viewModel);
            }
            this.SelectedChatSession = viewModel;
        }

        
        private void OnMessageReceived(ChatSession chatSession, string text)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                var viewModel = this.ChatSessions.FirstOrDefault(vm => vm.Thread == chatSession.Thread);
                if (viewModel == null)
                {
                    this.ChatSessions.Add(new ChatSessionViewModel(chatSession));
                }
                else
                {
                    viewModel.Refresh();
                }
            }).Wait();
        }

        private async Task<bool> HandleSubscriptionRequestReceivedAsync(string bareJid)
        {
            return await this.SubscriptionRequestInteraction.Handle(bareJid);
        }

        private async Task LoginAsync(CancellationToken ct)
        {
            var credentials = await LoginInteraction.Handle(Unit.Default);
            if (credentials != null)
            {
                await xmppClient.StartAsync(new Jid(credentials.Jid), credentials.Password);
                this.IsConnected = true;
            }
        }

        private async Task LogoutAsync(CancellationToken ct)
        {
            await this.xmppClient.ShutdownAsync();
        }

        private async Task AddRosterItemAsync(CancellationToken ct)
        {
            var rosterItemInfo = await AddRosterItemInteraction.Handle(Unit.Default);
            if (rosterItemInfo != null)
            {
                var b = await this.xmppClient.ProtocolHandler.RosterHandler.AddRosterItemAsync(rosterItemInfo.Jid, rosterItemInfo.Name, null);
                await this.xmppClient.ProtocolHandler.PresenceHandler.RequestSubscriptionAsync(rosterItemInfo.Jid);
            }
        }

        private async Task DeleteRosterItemAsync(CancellationToken ct)
        {
            if (this.SelectedRosterItem == null)
                return;

            await this.xmppClient.ProtocolHandler.RosterHandler.DeleteRosterItemAsync(this.SelectedRosterItem.Jid);
        }
    }
}
