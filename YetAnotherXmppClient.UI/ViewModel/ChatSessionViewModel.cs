using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using YetAnotherXmppClient.Protocol.Handler;

namespace YetAnotherXmppClient.UI.ViewModel
{
    public class ChatSessionViewModel : ReactiveObject, IEquatable<ChatSessionViewModel>
    {
        private readonly ChatSession session;

        public string Thread => this.session.Thread;
        public string OtherJid => this.session.OtherJid;
        public IEnumerable<string> Messages => this.session.Messages;

        private string textToSend;
        public string TextToSend
        {
            get => textToSend;
            set => this.RaiseAndSetIfChanged(ref this.textToSend, value);
        }

        public ReactiveCommand<Unit, Unit> SendCommand { get; }

        public ChatSessionViewModel(ChatSession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));

            this.SendCommand = ReactiveCommand.CreateFromTask(this.SendMessageAsync);
        }

        private async Task SendMessageAsync(CancellationToken ct)
        {
            await this.session.SendMessageAsync(this.TextToSend);
            this.TextToSend = string.Empty;
            this.RaisePropertyChanged(nameof(this.Messages));
        }

        public bool Equals(ChatSessionViewModel other)
        {
            return this.Thread == other?.Thread;
        }

        public void Refresh()
        {
            this.RaisePropertyChanged(nameof(this.Messages));
            this.RaisePropertyChanging(nameof(this.Messages));
        }
    }
}
