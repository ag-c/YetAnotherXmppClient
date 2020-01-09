using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using YetAnotherXmppClient.UI.ViewModel;

namespace YetAnotherXmppClient.UI.View
{
    public class ChatSessionControl : ReactiveUserControl<ChatSessionViewModel>
    {
        public Button SendButton => this.FindControl<Button>("sendButton");
        public ListBox MsgListBox => this.FindControl<ListBox>("msgListBox");
        public TextBox MessageTextBox => this.FindControl<TextBox>("messageTextBox");

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
                    this.MessageTextBox.GotFocus += this.HandleMessageTextBoxGotFocus;
                    this.MessageTextBox.LostFocus += HandleMessageTextBoxLostFocus;
                }
            );
        }

        private void HandleMessageTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            this.ViewModel.SendPausedChatStateNotification();
            e.Handled = true;
        }

        private void HandleMessageTextBoxGotFocus(object sender, GotFocusEventArgs e)
        {
            this.ViewModel.SendComposingChatStateNotification();
            e.Handled = true;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
