using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using YetAnotherXmppClient.UI.ViewModel;

namespace YetAnotherXmppClient.UI.View
{
    public class AddRosterItemWindow : ReactiveWindow<AddRosterItemWindow>
    {
        public string Jid { get; set; }
        public string ItemName { get; set; }

        public ICommand AddCommand { get; }
        public ICommand CancelCommand { get; }


        public AddRosterItemWindow()
        {
            this.AddCommand = ReactiveCommand.Create(() => this.Close(new RosterItemInfo { Jid = this.Jid, Name = this.ItemName }));
            this.CancelCommand = ReactiveCommand.Create(() => this.Close(null));
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
