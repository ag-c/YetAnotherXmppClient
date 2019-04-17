using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using YetAnotherXmppClient.UI.ViewModel;

namespace YetAnotherXmppClient.UI.View
{
    public class ChatSessionControl : ReactiveUserControl<ChatSessionViewModel>
    {
        public Button SendButton => this.FindControl<Button>("sendButton");
        public ListBox MsgListBox => this.FindControl<ListBox>("msgListBox");

        public ChatSessionControl()
        {
            this.InitializeComponent();
            this.WhenActivated(
                d =>
                {
                    d(this.BindCommand(this.ViewModel, x => x.SendCommand, x => x.SendButton));
                    this.OneWayBind(this.ViewModel,
                        viewModel => viewModel.Messages,
                        view => view.MsgListBox.Items);
                }
            );
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
