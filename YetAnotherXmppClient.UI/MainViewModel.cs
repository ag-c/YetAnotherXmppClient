using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using YetAnotherXmppClient.Core;

namespace YetAnotherXmppClient.UI
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private XmppClient xmppClient = new XmppClient();

        public StringWriter stringWriter = new StringWriter();
        public string LogText
        {
            get => stringWriter.ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand LoginCommand { get; }

        public MainViewModel()
        {
            this.LoginCommand = new ActionCommand(this.OnLoginCommandExecutedAsync);

            new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, OnTimer);
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
                await this.stringWriter.WriteLineAsync();
                await this.stringWriter.WriteLineAsync("OnLoginCommandExecutedAsync: " + e);
            }
        }
    }
}
