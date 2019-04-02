using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using StarDebris.Avalonia.MessageBox;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Protocol;
using YetAnotherXmppClient.Protocol.Handler;

namespace YetAnotherXmppClient.UI
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private XmppClient xmppClient = new XmppClient();

        public static StringWriter stringWriter = new StringWriter();
        private bool showAddRosterItemPopup;
        private DispatcherTimer timer;
        private IEnumerable<RosterItem> rosterItems;

        public string LogText
        {
            get => stringWriter.ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand LoginCommand { get; }
        public ICommand AddRosterItemCommand { get; }
        public ICommand DeleteRosterItemCommand { get; }


        public MainViewModel()
        {
            this.LoginCommand = new ActionCommand(this.OnLoginCommandExecutedAsync);
            this.AddRosterItemCommand = new ActionCommand(this.OnAddRosterItemCommandExecuted);
            this.DeleteRosterItemCommand = new ActionCommand(this.OnDeleteRosterItemCommandExecuted);

            this.xmppClient.RosterUpdated += this.HandleRosterUpdated;
            this.xmppClient.OnSubscriptionRequestReceived += this.HandleSubscriptionRequestReceivedAsync;

            this.timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, OnTimer);
            this.timer.Start();
        }

        private async Task<bool> HandleSubscriptionRequestReceivedAsync(string bareJid)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                new AskSubscriptionPermissionWindow().ShowDialog<bool>();
                //new MessageBox($"Allow {bareJid} to see your status?",
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
                rosterItems = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RosterItems)));
            }
        }

        public RosterItem SelectedRosterItem { get; set; }



        private void OnTimer(object sender, EventArgs e)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogText)));
        }

        private async void OnLoginCommandExecutedAsync(object sender)
        {
            try
            {
                await xmppClient.StartAsync(new Jid("yetanotherxmppuser@jabber.de/uiuiui"), "***");
            }
            catch (Exception e)
            {
                await stringWriter.WriteLineAsync();
                await stringWriter.WriteLineAsync("OnLoginCommandExecutedAsync: " + e);
            }
        }

        private async void OnAddRosterItemCommandExecuted(object sender)
        {
            var window = new AddRosterItemWindow();
            if(await window.ShowDialog<bool>())
                await this.xmppClient.ProtocolHandler.RosterHandler.AddRosterItemAsync(window.Jid, window.Name, new string[0]);
            //this.ShowAddRosterItemPopup = true;
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
                showAddRosterItemPopup = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowAddRosterItemPopup)));
            }
        }
    }
}
