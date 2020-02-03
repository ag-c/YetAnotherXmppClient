using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ReactiveUI;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Queries;
using YetAnotherXmppClient.Protocol.Handler;

namespace YetAnotherXmppClient.UI.ViewModel
{
    public class ChatSessionViewModel : ReactiveObject, IEquatable<ChatSessionViewModel>
    {
        private readonly ChatSession session;
        private readonly IMediator mediator;

        public string Thread => this.session.Thread;
        public string OtherJid => this.session.OtherJid;
        public ObservableCollection<ChatMessage> Messages { get; } = new ObservableCollection<ChatMessage>();

        private string textToSend;
        public string TextToSend
        {
            get => this.textToSend;
            set => this.RaiseAndSetIfChanged(ref this.textToSend, value);
        }

        private string otherChatState;
        public string OtherChatState
        {
            get => this.otherChatState;
            set => this.RaiseAndSetIfChanged(ref this.otherChatState, value);
        }

        public ReactiveCommand<Unit, Unit> SendCommand { get; }

        public ChatSessionViewModel(ChatSession session, IMediator mediator)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            this.session.NewMessage += this.HandleNewMessage;

            foreach (var message in session.Messages)
            {
                this.Messages.Add(message);
            }

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

        public void SendComposingChatStateNotification()
        {
            this.goInactiveCancellationTokenSource?.Cancel(false);
            this.mediator.ExecuteAsync(new SendChatStateNotificationCommand
                                           {
                                               FullJid = this.OtherJid,
                                               Thread = this.Thread,
                                               State = ChatState.composing
                                           });
        }

        private CancellationTokenSource goInactiveCancellationTokenSource;

        public async void SendPausedChatStateNotification()
        {
            this.mediator.ExecuteAsync(new SendChatStateNotificationCommand
                                           {
                                               FullJid = this.OtherJid,
                                               Thread = this.Thread,
                                               State = ChatState.paused
                                           });
            this.goInactiveCancellationTokenSource = new CancellationTokenSource();
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), this.goInactiveCancellationTokenSource.Token);

                this.mediator.ExecuteAsync(new SendChatStateNotificationCommand
                                               {
                                                   FullJid = this.OtherJid,
                                                   Thread = this.Thread,
                                                   State = ChatState.inactive
                                               });
            }
            catch
            {
                // going inactive has been canceled
            }
            finally
            {
                this.goInactiveCancellationTokenSource.Dispose();
                this.goInactiveCancellationTokenSource = null;
            }
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
