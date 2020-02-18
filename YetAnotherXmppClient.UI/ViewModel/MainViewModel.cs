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
using Serilog;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.StanzaParts;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Commands;
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
                                 IAsyncQueryHandler<SubscriptionRequestQuery, bool>, 
                                 IEventHandler<StreamNegotiationCompletedEvent>,
                                 IEventHandler<MessageReceivedEvent>,
                                 IEventHandler<ChatStateNotificationReceivedEvent>
    {
        private XmppClient xmppClient;

        private TextWriter logWriter;

        string IRoutableViewModel.UrlPathSegment { get; } = "main";
        IScreen IRoutableViewModel.HostScreen { get; }

        public ReactiveCommand<Unit,Unit> ShowPreferencesCommand { get; }
        public ReactiveCommand<Unit,Unit> ShowServiceDiscoveryCommand { get; }
        public ReactiveCommand<Unit,Unit> ShowBlockingCommand { get; }
        public ReactiveCommand<Unit,Unit> ShowPrivateXmlStorageCommand { get; }
        public ReactiveCommand<Unit,Unit> ShowMoodCommand { get; }
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

        private bool isBlockingFeatureSupported;
        public bool IsBlockingFeatureSupported
        {
            get => this.isBlockingFeatureSupported;
            set => this.RaiseAndSetIfChanged(ref this.isBlockingFeatureSupported, value);
        }        
        
        private bool isPrivateXmlStorageFeatureSupported;
        public bool IsPrivateXmlStorageFeatureSupported
        {
            get => this.isPrivateXmlStorageFeatureSupported;
            set => this.RaiseAndSetIfChanged(ref this.isPrivateXmlStorageFeatureSupported, value);
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

        public string SelectedPresenceShowValue
        {
            set
            {
                this.xmppClient.ExecuteAsync(new BroadcastPresenceCommand
                                                 {
                                                     Show = Enum.Parse<PresenceShow>(value)
                                                 });
            }
        }


        public MainViewModel(XmppClient xmppClient, TextWriter logWriter)
        {
            this.xmppClient = xmppClient;

            this.Roster = new RosterViewModel(this.xmppClient, logWriter)
                              {
                                  OnInitiateChatSession = this.OnInitiateChatSession
                              };

            this.LogoutCommand = ReactiveCommand.CreateFromTask(() => this.OnLogoutRequested?.Invoke());
            this.LogoutCommand.ThrownExceptions.Subscribe(ex => PrintException("MainViewModel.LogoutAsync", ex));

            this.ShowPreferencesCommand = ReactiveCommand.CreateFromTask(this.ShowPreferencesAsync);
            this.ShowServiceDiscoveryCommand = ReactiveCommand.CreateFromTask(this.ShowServiceDiscoveryAsync);
            this.ShowBlockingCommand = ReactiveCommand.CreateFromTask(this.ShowBlockingAsync);
            this.ShowPrivateXmlStorageCommand = ReactiveCommand.CreateFromTask(this.ShowPrivateXmlStorageAsync);
            this.ShowMoodCommand = ReactiveCommand.CreateFromTask(this.ShowMoodAsync);


            this.xmppClient.Disconnected += this.HandleDisconnected;

            this.xmppClient.RegisterHandler<StreamNegotiationCompletedEvent>(this, publishLatestEventToNewHandler: true);
            this.xmppClient.RegisterHandler<MessageReceivedEvent>(this);
            this.xmppClient.RegisterHandler<ChatStateNotificationReceivedEvent>(this);
            this.xmppClient.RegisterHandler<SubscriptionRequestQuery, bool>(this);


            async Task PrintException(string location, Exception exception)
            {
                await this.logWriter.WriteAndFlushAsync(location + ": " + exception);
            }
        }

        private async Task ShowPreferencesAsync(CancellationToken ct)
        {
            await Interactions.ShowPreferences.Handle(this.xmppClient);
        }

        private async Task ShowServiceDiscoveryAsync(CancellationToken ct)
        {
            await Interactions.ShowServiceDiscovery.Handle((this.xmppClient, null));
        }

        private async Task ShowBlockingAsync(CancellationToken ct)
        {
            await Interactions.ShowBlocking.Handle(this.xmppClient);
        }        
        
        private async Task ShowPrivateXmlStorageAsync(CancellationToken ct)
        {
            await Interactions.ShowPrivateXmlStorage.Handle(this.xmppClient);
        }

        private async Task ShowMoodAsync(CancellationToken ct)
        {
            var (mood, text) = await Interactions.ShowMood.Handle(Unit.Default);
            await this.xmppClient.ExecuteAsync(new SetMoodCommand
                                                   {
                                                       Mood = mood,
                                                       Text = text
                                                   });
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
                //var chatSession = this.xmppClient.ProtocolHandler.Get<ImProtocolHandler>().StartChatSession(jid);
                var chatSession = this.xmppClient.Query<StartChatSessionQuery, ChatSession>(jid);
                viewModel = new ChatSessionViewModel(chatSession, this.xmppClient);
                this.ChatSessions.Add(viewModel);
            }
            this.SelectedChatSession = viewModel;
        }

        async Task<bool> IAsyncQueryHandler<SubscriptionRequestQuery, bool>.HandleQueryAsync(SubscriptionRequestQuery query)
        {
            return await Interactions.SubscriptionRequest.Handle(query.BareJid);
        }

        async Task IEventHandler<StreamNegotiationCompletedEvent>.HandleEventAsync(StreamNegotiationCompletedEvent evt)
        {
            this.ConnectedJid = evt.ConnectedJid;
            this.IsProtocolNegotiationComplete = true;
            this.IsBlockingFeatureSupported = await this.xmppClient.IsFeatureSupportedAsync(ProtocolNamespaces.Blocking);
            this.IsPrivateXmlStorageFeatureSupported = await this.xmppClient.IsFeatureSupportedAsync(ProtocolNamespaces.PrivateXmlStorage);
        }

        Task IEventHandler<MessageReceivedEvent>.HandleEventAsync(MessageReceivedEvent evt)
        {
            return Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var viewModel = this.ChatSessions.FirstOrDefault(vm => vm.Thread == evt.Session.Thread);
                    if (viewModel == null)
                    {
                        this.ChatSessions.Add(new ChatSessionViewModel(evt.Session, this.xmppClient));
                    }
                    else
                    {
                        viewModel.Refresh();
                    }
                });
        }

        public void HandleSessionActivation(ChatSessionViewModel chatSessionViewModel, bool activated)
        {
            this.xmppClient.ExecuteAsync(new SendChatStateNotificationCommand
                                             {
                                                 FullJid = chatSessionViewModel.OtherJid,
                                                 Thread = chatSessionViewModel.Thread,
                                                 State = activated ? ChatState.active : ChatState.inactive
                                             });
        }

        Task IEventHandler<ChatStateNotificationReceivedEvent>.HandleEventAsync(ChatStateNotificationReceivedEvent evt)
        {
            return Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var viewModel = this.ChatSessions.FirstOrDefault(vm => vm.OtherJid == evt.FullJid);
                    if (viewModel == null)
                    {
                        Log.Debug($"Received chat state notification for full jid '{evt.FullJid}' without open chat session view model");
                    }
                    else
                    {
                        viewModel.OtherChatState = evt.State.ToString();
                    }
                });
        }

        public void AttestActivity()
        {
            this.xmppClient.Execute(new AttestActivityCommand());
        }
    }
}
