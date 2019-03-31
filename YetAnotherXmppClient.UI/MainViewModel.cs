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


        public MainViewModel()
        {
            this.LoginCommand = new ActionCommand(this.OnLoginCommandExecutedAsync);
            this.AddRosterItemCommand = new ActionCommand(this.OnAddRosterItemCommandExecuted);

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


        private void OnTimer(object sender, EventArgs e)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogText)));
        }

        private async void OnLoginCommandExecutedAsync(object sender)
        {
            try
            {
                await xmppClient.StartAsync(new Jid("yetanotherxmppuser@jabber.de/uiuiui"), "gehe1m");
            }
            catch (Exception e)
            {
                await stringWriter.WriteLineAsync();
                await stringWriter.WriteLineAsync("OnLoginCommandExecutedAsync: " + e);
            }
        }

        private void OnAddRosterItemCommandExecuted(object sender)
        {
            new AddRosterItemWindow().ShowDialog<bool>();
            this.ShowAddRosterItemPopup = true;
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
