using System;
using ReactiveUI;

namespace YetAnotherXmppClient.UI.ViewModel
{
    public class LoginViewModel : ReactiveObject
    {
        public ReactiveCommand LoginCommand { get; }
        public ReactiveCommand CancelCommand { get; }

        public Action<LoginCredentials> CloseAction { get; set; }

        private string jid;
        public string Jid
        {
            get => jid;
            set => this.RaiseAndSetIfChanged(ref this.jid, value);
        }

        public string Password { get; set; }

        public LoginViewModel()
        {
            var canExecute = this.WhenAnyValue(x => x.Jid, jid => !string.IsNullOrWhiteSpace(jid) && jid.Contains("@"));
            this.LoginCommand = ReactiveCommand.Create(() => this.CloseAction(new LoginCredentials { Jid = this.Jid, Password = this.Password }), canExecute);
            this.CancelCommand = ReactiveCommand.Create(() => this.CloseAction(null));
        }
    }
}
