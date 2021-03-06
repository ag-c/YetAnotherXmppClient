﻿using System;
using System.IO;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Extensions;

namespace YetAnotherXmppClient.UI.ViewModel
{
    public class LoginViewModel : ReactiveObject, IRoutableViewModel
    {
        private readonly XmppClient xmppClient;

        string IRoutableViewModel.UrlPathSegment { get; } = "login";
        IScreen IRoutableViewModel.HostScreen { get; }

        public ReactiveCommand<Unit, Unit> LoginCommand { get; }
        //public ReactiveCommand CancelCommand { get; }

        public Action<LoginCredentials> CloseAction { get; set; }

        private string jid;
        public string Jid
        {
            get => this.jid;
            set => this.RaiseAndSetIfChanged(ref this.jid, value);
        }

        public string Password { get; set; }

        public LoginViewModel(XmppClient xmppClient, TextWriter logWriter)
        {
            this.xmppClient = xmppClient;

            var canExecute = this.WhenAnyValue(x => x.Jid, jid => !string.IsNullOrWhiteSpace(jid) && jid.Contains("@"));
            //this.LoginCommand = ReactiveCommand.Create(() => this.CloseAction(new LoginCredentials { Jid = this.Jid, Password = this.Password }), canExecute);
            this.LoginCommand = ReactiveCommand.CreateFromTask(this.OnLogin, canExecute);
            this.LoginCommand.ThrownExceptions.Subscribe(async ex => await logWriter.WriteAndFlushAsync("Error occurred while starting xmpp client: " + ex));
            //this.CancelCommand = ReactiveCommand.Create(() => this.CloseAction(null));

            //this.Jid = "yetanotherxmppuser@wiuwiu.de/uiuiui";
            //this.Jid = "yetanotherxmppuser@chat.sum7.eu/uiuiui"; //gehe1m?
            //this.Jid = "yetanotherxmppuser@xmpp.is/uiuiui"; //gehe1mgehe1m
            this.Jid = "yetanotherxmppuser@jabber.de/uiuiui";
            this.Password = "gehe1m";
            this.RaisePropertyChanged(nameof(this.Password));
        }

        private async Task OnLogin(CancellationToken ct)
        {
            await this.xmppClient.StartAsync(new Jid(this.jid), this.Password);
        }
    }
}
