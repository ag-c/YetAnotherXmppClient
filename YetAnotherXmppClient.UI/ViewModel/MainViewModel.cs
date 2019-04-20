using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.StanzaParts;
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
        public static DebugTextWriterDecorator LogWriter = new DebugTextWriterDecorator(new StringWriter(), _ => instance?.RaisePropertyChanged(nameof(LogText)));

        public string LogText => LogWriter.Decoratee.ToString();

        public ReactiveCommand LoginCommand { get; }
        public ReactiveCommand LogoutCommand { get; }


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

        public ObservableCollection<ChatSessionViewModel> ChatSessions { get; } = new ObservableCollection<ChatSessionViewModel>();

        private ChatSessionViewModel selectedChatSession;
        public ChatSessionViewModel SelectedChatSession
        {
            get => this.selectedChatSession;
            set => this.RaiseAndSetIfChanged(ref this.selectedChatSession, value);
        }

        public RosterViewModel Roster { get; }

        public IEnumerable<string> PresenceShowValues => Enum.GetNames(typeof(PresenceShow));

        public MainViewModel()
        {
            instance = this;

            this.Roster = new RosterViewModel(this.xmppClient, LogWriter)
                              {
                                  OnInitiateChatSession = this.OnInitiateChatSession
                              };

            this.LoginCommand = ReactiveCommand.CreateFromTask(this.LoginAsync);//new ActionCommand(this.OnLoginCommandExecutedAsync);
            this.LoginCommand.ThrownExceptions.Subscribe(ex => PrintException("MainViewModel.LoginAsync", ex));
            this.LogoutCommand = ReactiveCommand.CreateFromTask(this.LogoutAsync);
            this.LogoutCommand.ThrownExceptions.Subscribe(ex => PrintException("MainViewModel.LogoutAsync", ex));


            this.xmppClient.Disconnected += this.HandleDisconnected;
            this.xmppClient.ProtocolNegotiationFinished += (sender, connectedJid) =>
            {
                this.ConnectedJid = connectedJid;
                this.IsProtocolNegotiationComplete = true;
            };

            this.xmppClient.SubscriptionRequestReceived += this.HandleSubscriptionRequestReceivedAsync;
            this.xmppClient.MessageReceived = this.OnMessageReceived;
            

            async Task PrintException(string location, Exception exception)
            {
                await LogWriter.WriteAndFlushAsync(location + ": " + exception);
            }
        }

        private void HandleDisconnected(object sender, EventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.IsConnected = false;
                    this.IsProtocolNegotiationComplete = false;
                    this.ConnectedJid = null;
                    this.ChatSessions.Clear();
                });
        }

        private void OnInitiateChatSession(Jid jid)
        {
            // is there already a session with the same jid? //UNDONE fulljid
            var viewModel = this.ChatSessions.FirstOrDefault(vm => vm.OtherJid == jid);
            if (viewModel == null)
            {
                var chatSession = this.xmppClient.ProtocolHandler.ImProtocolHandler.StartChatSession(jid);
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
            return await Interactions.SubscriptionRequest.Handle(bareJid);
        }

        private async Task LoginAsync(CancellationToken ct)
        {
            var credentials = await Interactions.Login.Handle(Unit.Default);
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
    }
}
