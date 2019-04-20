using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using YetAnotherXmppClient.UI.ViewModel;

namespace YetAnotherXmppClient.UI.View
{
    public class LoginView : ReactiveUserControl<LoginViewModel>
    {
        public LoginView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
