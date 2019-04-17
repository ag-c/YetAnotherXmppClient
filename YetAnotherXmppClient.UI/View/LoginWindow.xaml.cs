using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using YetAnotherXmppClient.UI.ViewModel;

namespace YetAnotherXmppClient.UI.View
{
    public class LoginWindow : ReactiveWindow<LoginViewModel>, IViewFor<LoginViewModel>
    {
        public Button LoginButton => this.FindControl<Button>("loginButton");
        public Button CancelButton => this.FindControl<Button>("cancelButton");
        public TextBox JidTextBox => this.FindControl<TextBox>("jidTextBox");

        public LoginWindow()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            this.ViewModel = new LoginViewModel();
            this.WhenActivated(
                d =>
                {
                    d(this.BindCommand(this.ViewModel, x => x.LoginCommand, x => x.LoginButton));
                    d(this.BindCommand(this.ViewModel, x => x.CancelCommand, x => x.CancelButton));
                    d(this.Bind(this.ViewModel, vm => vm.Jid, v => v.JidTextBox.Text));

                    this.ViewModel.CloseAction = credentials => this.Close(credentials);
                });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
