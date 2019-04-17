using System;
using ReactiveUI;

namespace YetAnotherXmppClient.UI.ViewModel
{
    public class LoginViewModel : ReactiveObject, ISupportsActivation
    {
        public ReactiveCommand LoginCommand { get; }
        public ReactiveCommand CancelCommand { get; }

        public Action<LoginCredentials> CloseAction { get; set; }

        public string Jid { get; set; }
        public string Password { get; set; }

        public LoginViewModel()
        {
            this.Activator = new ViewModelActivator();
            this.LoginCommand = ReactiveCommand.Create(() => this.CloseAction(new LoginCredentials { Jid = this.Jid, Password = this.Password }));
            this.CancelCommand = ReactiveCommand.Create(() => this.CloseAction(null));
        }

        public ViewModelActivator Activator { get; }
    }
}
