using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ReactiveUI;
using YetAnotherXmppClient.Protocol.Handler;

namespace YetAnotherXmppClient.UI.ViewModel
{
    public class ChatSessionViewModel : ReactiveObject, IEquatable<ChatSessionViewModel>
    {
        private readonly ChatSession session;

        public string Thread => this.session.Thread;
        public string OtherJid => this.session.OtherJid;
        public ObservableCollection<ChatMessage> Messages { get; } = new ObservableCollection<ChatMessage>();

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
            this.session.NewMessage += this.HandleNewMessage;

            this.SendCommand = ReactiveCommand.CreateFromTask(this.SendMessageAsync);
        }

        private void HandleNewMessage(object sender, ChatMessage message)
        {
            Dispatcher.UIThread.InvokeAsync(() => this.Messages.Add(message));
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
