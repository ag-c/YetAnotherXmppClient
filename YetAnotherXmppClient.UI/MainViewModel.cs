using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using StarDebris.Avalonia.MessageBox;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Protocol;
using YetAnotherXmppClient.Protocol.Handler;

namespace YetAnotherXmppClient.UI
{
    public class RosterItemInfo
    {
        public string Jid { get; set; }
        public string Name { get; set; }
    }
    public class MainViewModel : ReactiveObject
    {
        private XmppClient xmppClient = new XmppClient();

        private static MainViewModel instance;
        public static DebugTextWriterDecorator stringWriter = new DebugTextWriterDecorator(new StringWriter(), _ => instance?.RaisePropertyChanged(nameof(LogText)));
        private bool showAddRosterItemPopup;
        private DispatcherTimer timer;
        private IEnumerable<RosterItem> rosterItems;
        private bool isProtocolNegotiationComplete;

        public Interaction<Unit, RosterItemInfo> AddRosterItemInteraction { get; } = new Interaction<Unit, RosterItemInfo>();

        public string LogText => stringWriter.Decoratee.ToString();

        public ReactiveCommand LoginCommand { get; }
        public ICommand AddRosterItemCommand { get; }
        public ICommand DeleteRosterItemCommand { get; }

        public bool IsProtocolNegotiationComplete
        {
            get => isProtocolNegotiationComplete;
            set => this.RaiseAndSetIfChanged(ref this.isProtocolNegotiationComplete, value);
        }


        public MainViewModel()
        {
            instance = this;

            this.LoginCommand = ReactiveCommand.CreateFromTask(this.LoginAsync);//new ActionCommand(this.OnLoginCommandExecutedAsync);
            this.LoginCommand.ThrownExceptions.Subscribe(async exception =>
            {
                await stringWriter.WriteLineAsync();
                await stringWriter.WriteLineAsync("OnLoginCommandExecutedAsync: " + exception);
            });
            this.AddRosterItemCommand = new ActionCommand(this.OnAddRosterItemCommandExecuted);
            this.DeleteRosterItemCommand = new ActionCommand(this.OnDeleteRosterItemCommandExecuted);

            this.xmppClient.ProtocolNegotiationFinished += (sender, args) => this.IsProtocolNegotiationComplete = true;
            this.xmppClient.RosterUpdated += this.HandleRosterUpdated;
            this.xmppClient.SubscriptionRequestReceived += this.HandleSubscriptionRequestReceivedAsync;
            this.xmppClient.MessageReceived = OnMessageReceived;
        }

        private void OnMessageReceived(ChatSession chatSession, Jid arg1, string arg2)
        {
            throw new NotImplementedException();
        }

        private async Task<bool> HandleSubscriptionRequestReceivedAsync(string bareJid)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                //new AskSubscriptionPermissionWindow().ShowDialog<bool>();
                ////new MessageBox($"Allow {bareJid} to see your status?",
                //    (dialogResult, e) => { tcs.SetResult(dialogResult.result == MessageBoxButtons.Yes); },
                //    MessageBoxStyle.Info, MessageBoxButtons.Yes | MessageBoxButtons.No).Show();
            });

            return await tcs.Task;
        }

        private void HandleRosterUpdated(object sender, IEnumerable<RosterItem> rosterItems)
        {
            this.RosterItems = rosterItems;
        }

        public IEnumerable<RosterItem> RosterItems
        {
            get => rosterItems;
            set
            {
                this.RaiseAndSetIfChanged(ref this.rosterItems, value);
                //rosterItems = value;
                //this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RosterItems)));
            }
        }

        public RosterItem SelectedRosterItem { get; set; }



        private void OnTimer(object sender, EventArgs e)
        {
            //this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogText)));
            this.RaisePropertyChanged(nameof(LogText));
        }

        private async Task LoginAsync(CancellationToken ct)//object sender)
        {
            //try
            //{
                await xmppClient.StartAsync(new Jid("yetanotherxmppuser@wiuwiu.de/uiuiui"), "gehe1m");
            //}
            //catch (Exception e)
            //{
            //    await stringWriter.WriteLineAsync();
            //    await stringWriter.WriteLineAsync("OnLoginCommandExecutedAsync: " + e);
            //}
        }

        private async void OnAddRosterItemCommandExecuted(object sender)
        {
            //var window = new AddRosterItemWindow();
            //if(await window.ShowDialog<bool>())
            //await this.xmppClient.ProtocolHandler.RosterHandler.AddRosterItemAsync(window.Jid, window.Name, new string[0]);
            //this.ShowAddRosterItemPopup = true;

            var rosterItemInfo = await AddRosterItemInteraction.Handle(Unit.Default);

            if (rosterItemInfo != null)
            {
                var b = await this.xmppClient.ProtocolHandler.RosterHandler.AddRosterItemAsync(rosterItemInfo.Jid, rosterItemInfo.Name, null);
            }
        }

        private async void OnDeleteRosterItemCommandExecuted(object sender)
        {
            if (this.SelectedRosterItem == null)
                return;

            await this.xmppClient.ProtocolHandler.RosterHandler.DeleteRosterItemAsync(this.SelectedRosterItem.Jid);
        }

        public bool ShowAddRosterItemPopup
        {
            get => showAddRosterItemPopup;
            set
            {
                //showAddRosterItemPopup = value;
                //this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowAddRosterItemPopup)));
            }
        }
    }
}
