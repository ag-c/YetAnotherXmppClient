using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace YetAnotherXmppClient.UI
{
    public class AddRosterItemWindow : Window
    {
        public string Jid { get; set; }
        public string ItemName { get; set; }

        public ICommand AddCommand { get; }
        public ICommand CancelCommand { get; }


        public AddRosterItemWindow()
        {
            this.AddCommand = new ActionCommand(this.OnAddCommandExecuted);
            this.CancelCommand = new ActionCommand(this.OnCancelCommandExecuted);
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void OnAddCommandExecuted(object obj)
        {
            this.Close(new RosterItemInfo {Jid = this.Jid, Name = this.ItemName});
        }

        private void OnCancelCommandExecuted(object obj)
        {
            this.Close(null);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
