using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace YetAnotherXmppClient.UI
{
    public class ChatSessionControl : UserControl
    {
        public ChatSessionControl()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
