using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace YetAnotherXmppClient.UI
{
    public class AddRosterItemWindow : Window
    {
        public string Jid { get; set; }
        public new string Name { get; set; }

        public ICommand AddCommand { get; }
        public ICommand CancelCommand { get; }


        public AddRosterItemWindow()
        {
            this.InitializeComponent();
            this.AddCommand = new ActionCommand(OnAddCommandExecuted);
            this.CancelCommand = new ActionCommand(this.OnCancelCommandExecuted);
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void OnAddCommandExecuted(object obj)
        {
            this.Close(true);
        }

        private void OnCancelCommandExecuted(object obj)
        {
            this.Close(false);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
