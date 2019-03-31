using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace YetAnotherXmppClient.UI
{
    public class AskSubscriptionPermissionWindow : Window
    {
        public string RequestingJid { get; set; } = "test123";

        public AskSubscriptionPermissionWindow()
        {
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
