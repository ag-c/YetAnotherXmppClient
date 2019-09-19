using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ReactiveUI;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.StanzaParts;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Events;
using YetAnotherXmppClient.Infrastructure.Queries;
using YetAnotherXmppClient.Protocol.Handler;
using RosterItem = YetAnotherXmppClient.Protocol.Handler.RosterItem;
using Unit = System.Reactive.Unit;

namespace YetAnotherXmppClient.UI.ViewModel
{
    public class RosterItemWithAvatarViewModel : ReactiveObject
    {
        private IBitmap avatar;
        public IBitmap Avatar
        {
            get => this.avatar;
            set => this.RaiseAndSetIfChanged(ref this.avatar, value);
        }

        public string Jid { get; set; }
        public string Name { get; set; }
        public IEnumerable<string> Groups { get; set; }
        public SubscriptionState Subscription { get; set; }
        public bool IsSubscriptionPending { get; set; }

        private bool isOnline;
        public bool IsOnline
        {
            get => this.isOnline;
            set
            {
                this.RaiseAndSetIfChanged(ref this.isOnline, value);
            }
        }
    }

    public class RosterViewModel : ReactiveObject, 
        IEventHandler<RosterUpdateEvent>, 
        IEventHandler<StreamNegotiationCompletedEvent>,
        IEventHandler<AvatarReceivedEvent>,
        IEventHandler<PresenceEvent>
    {
        private readonly XmppClient xmppClient;

        private RosterItemWithAvatarViewModel[] rosterItems;

        public RosterItemWithAvatarViewModel[] RosterItems
        {
            get => this.rosterItems;
            set => this.RaiseAndSetIfChanged(ref this.rosterItems, value);
        }

        public RosterItemWithAvatarViewModel SelectedRosterItem { get; set; }

        public ReactiveCommand StartChatCommand { get; }
        public ReactiveCommand AddRosterItemCommand { get; }
        public ReactiveCommand DeleteRosterItemCommand { get; }

        public Action<Jid> OnInitiateChatSession { get; set; }

        public RosterViewModel(XmppClient xmppClient, TextWriter logWriter)
        {
            this.xmppClient = xmppClient;

            //var canExecute = this.WhenAny(x => x.SelectedRosterItem, selection => selection != null); //UNDONE
            this.StartChatCommand = ReactiveCommand.Create(this.InitiateChatSession);
            this.StartChatCommand.ThrownExceptions.Subscribe(ex => PrintException("MainViewModel.StartChatSession", ex));
            this.AddRosterItemCommand = ReactiveCommand.CreateFromTask(this.AddRosterItemAsync);
            this.DeleteRosterItemCommand = ReactiveCommand.CreateFromTask(this.DeleteRosterItemAsync);

            this.xmppClient.Disconnected += this.HandleDisconnected;

            this.xmppClient.RegisterHandler<StreamNegotiationCompletedEvent>(this, publishLatestEventToNewHandler: true);
            this.xmppClient.RegisterHandler<RosterUpdateEvent>(this, publishLatestEventToNewHandler: true);
            this.xmppClient.RegisterHandler<PresenceEvent>(this);

            async Task PrintException(string location, Exception exception)
            {
                await logWriter.WriteAndFlushAsync(location + ": " + exception);
            }
        }

        Task IEventHandler<RosterUpdateEvent>.HandleEventAsync(RosterUpdateEvent rosterUpdate)
        {
            this.RosterItems = rosterUpdate.Items.Select(x => new RosterItemWithAvatarViewModel
                                                                  {
                                                                      Jid = x.Jid,
                                                                      Name = x.Name,
                                                                      Subscription = x.Subscription,
                                                                      Groups = x.Groups,
                                                                      IsSubscriptionPending = x.IsSubscriptionPending,
                                                                  }).ToArray();
            return Task.CompletedTask;
        }

        private void HandleDisconnected(object sender, EventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.RosterItems = null;
                });
        }

        private void InitiateChatSession()
        {
            if (this.SelectedRosterItem == null)
                return;

            this.OnInitiateChatSession?.Invoke(new Jid(this.SelectedRosterItem.Jid));
        }

        private async Task AddRosterItemAsync(CancellationToken ct)
        {
            var rosterItemInfo = await Interactions.AddRosterItem.Handle(Unit.Default);
            if (rosterItemInfo != null)
            {
                var b1 = await this.xmppClient.QueryAsync<AddRosterItemQuery, bool>(new AddRosterItemQuery
                {
                    BareJid = rosterItemInfo.Jid,
                    Name = rosterItemInfo.Name,
                    Groups = null
                });
                var b2 = await this.xmppClient.QueryAsync<RequestSubscriptionQuery, bool>(new RequestSubscriptionQuery
                {
                    Jid = rosterItemInfo.Jid,
                });
            }
        }

        private async Task DeleteRosterItemAsync(CancellationToken ct)
        {
            if (this.SelectedRosterItem == null)
                return;

            await this.xmppClient.QueryAsync<DeleteRosterItemQuery, bool>(new DeleteRosterItemQuery {BareJid = this.SelectedRosterItem.Jid});
        }

        Task IEventHandler<StreamNegotiationCompletedEvent>.HandleEventAsync(StreamNegotiationCompletedEvent evt)
        {
            this.xmppClient.RegisterHandler<AvatarReceivedEvent>(this);
            return Task.CompletedTask;
        }

        Task IEventHandler<AvatarReceivedEvent>.HandleEventAsync(AvatarReceivedEvent evt)
        {
            var item = this.RosterItems.FirstOrDefault(x => x.Jid == evt.BareJid);
            if (item != null)
                item.Avatar = new Bitmap(new MemoryStream(evt.Bytes));

            return Task.CompletedTask;
        }

        Task IEventHandler<PresenceEvent>.HandleEventAsync(PresenceEvent evt)
        {
            var item = this.RosterItems.FirstOrDefault(ri => ri.Jid == evt.Jid.Bare);
            if (item != null)
            {
                Dispatcher.UIThread.InvokeAsync(() => item.IsOnline = evt.IsAvailable);
            }

            return Task.CompletedTask;
        }
    }
}
