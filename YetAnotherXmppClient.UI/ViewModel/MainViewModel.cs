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
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Events;
using YetAnotherXmppClient.Infrastructure.Queries;
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
    public class MainViewModel : ReactiveObject, IRoutableViewModel, 
                                 IQueryHandler<SubscriptionRequestQuery, bool>, 
                                 IEventHandler<StreamNegotiationCompletedEvent>,
                                 IEventHandler<MessageReceivedEvent>
    {
        private XmppClient xmppClient;

        private TextWriter logWriter;

        string IRoutableViewModel.UrlPathSegment { get; } = "main";
        IScreen IRoutableViewModel.HostScreen { get; }

        public ReactiveCommand<Unit,Unit> ShowServiceDiscoveryCommand { get; }
        public ReactiveCommand<Unit,Unit> LogoutCommand { get; }
        public Func<Task> OnLogoutRequested { get; set; }


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


        public MainViewModel(XmppClient xmppClient, TextWriter logWriter)
        {
            this.xmppClient = xmppClient;

            this.Roster = new RosterViewModel(this.xmppClient, logWriter)
                              {
                                  OnInitiateChatSession = this.OnInitiateChatSession
                              };

            this.LogoutCommand = ReactiveCommand.CreateFromTask(() => this.OnLogoutRequested?.Invoke());
            this.LogoutCommand.ThrownExceptions.Subscribe(ex => PrintException("MainViewModel.LogoutAsync", ex));

            this.ShowServiceDiscoveryCommand = ReactiveCommand.CreateFromTask(this.ShowServiceDiscoveryAsync);


            this.xmppClient.Disconnected += this.HandleDisconnected;

            this.xmppClient.Mediator.RegisterHandler<StreamNegotiationCompletedEvent>(this, publishLatestEventToNewHandler: true);
            this.xmppClient.Mediator.RegisterHandler<MessageReceivedEvent>(this);
            this.xmppClient.Mediator.RegisterHandler<SubscriptionRequestQuery, bool>(this);


            async Task PrintException(string location, Exception exception)
            {
                await this.logWriter.WriteAndFlushAsync(location + ": " + exception);
            }
        }

        private async Task ShowServiceDiscoveryAsync(CancellationToken ct)
        {
            await Interactions.ShowServiceDiscovery.Handle(this.xmppClient.ProtocolHandler.ServiceDiscoveryHandler);
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

        async Task<bool> IQueryHandler<SubscriptionRequestQuery, bool>.HandleQueryAsync(SubscriptionRequestQuery query)
        {
            return await Interactions.SubscriptionRequest.Handle(query.BareJid);
        }


        //private async Task LogoutAsync(CancellationToken ct)
        //{
        //    await this.xmppClient.ShutdownAsync();
        //}

        Task IEventHandler<StreamNegotiationCompletedEvent>.HandleEventAsync(StreamNegotiationCompletedEvent evt)
        {
            this.ConnectedJid = evt.ConnectedJid;
            this.IsProtocolNegotiationComplete = true;
            return Task.CompletedTask;
        }

        Task IEventHandler<MessageReceivedEvent>.HandleEventAsync(MessageReceivedEvent evt)
        {
            return Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var viewModel = this.ChatSessions.FirstOrDefault(vm => vm.Thread == evt.Session.Thread);
                    if (viewModel == null)
                    {
                        this.ChatSessions.Add(new ChatSessionViewModel(evt.Session));
                    }
                    else
                    {
                        viewModel.Refresh();
                    }
                });
        }
    }
}
