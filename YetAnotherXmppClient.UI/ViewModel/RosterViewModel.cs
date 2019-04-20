﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using JetBrains.Annotations;
using ReactiveUI;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Protocol.Handler;

namespace YetAnotherXmppClient.UI.ViewModel
{
    public class RosterItemWithAvatar : RosterItem, INotifyPropertyChanged 
    {
        private IBitmap avatar;

        public IBitmap Avatar
        {
            get => this.avatar;
            set
            {
                this.avatar = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Avatar)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class RosterViewModel : ReactiveObject
    {
        private readonly XmppClient xmppClient;

        private IEnumerable<RosterItemWithAvatar> rosterItems;

        public IEnumerable<RosterItemWithAvatar> RosterItems
        {
            get => this.rosterItems?.ToArray();
            set => this.RaiseAndSetIfChanged(ref this.rosterItems, value);
        }

        public RosterItemWithAvatar SelectedRosterItem { get; set; }

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
            this.xmppClient.ProtocolNegotiationFinished += (sender, connectedJid) =>
                {
                    this.xmppClient.ProtocolHandler.Get<VCardProtocolHandler>().AvatarReceived += this.AvatarReceived; 
                };
            this.xmppClient.RosterUpdated += (sender, items) => this.RosterItems = items.Select(x => new RosterItemWithAvatar
            {
                                                                                                         Jid = x.Jid,
                                                                                                         Name = x.Name,
                                                                                                         Subscription = x.Subscription,
                                                                                                         Groups = x.Groups,
                                                                                                         IsSubscriptionPending = x.IsSubscriptionPending
                                                                                                     });

            async Task PrintException(string location, Exception exception)
            {
                await logWriter.WriteAndFlushAsync(location + ": " + exception);
            }
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

        private void AvatarReceived(object sender, (string BareJid, byte[] Bytes) e)
        {
            var item = this.RosterItems.FirstOrDefault(x => x.Jid == e.BareJid);
            if (item != null)
                item.Avatar = new Bitmap(new MemoryStream(e.Bytes));
        }
    }
}