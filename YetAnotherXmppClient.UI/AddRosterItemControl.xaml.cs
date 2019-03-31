using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace YetAnotherXmppClient.UI
{
    public class AddRosterItemControl : UserControl
    {
        public AddRosterItemControl()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
