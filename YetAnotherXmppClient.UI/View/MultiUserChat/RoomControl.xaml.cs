using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using YetAnotherXmppClient.UI.ViewModel.MultiUserChat;

namespace YetAnotherXmppClient.UI.View.MultiUserChat
{
    public class RoomControl : ReactiveUserControl<RoomViewModel>
    {
        public RoomControl()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
