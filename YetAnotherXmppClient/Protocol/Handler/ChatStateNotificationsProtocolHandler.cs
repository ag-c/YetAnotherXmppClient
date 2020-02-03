using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Commands;
using YetAnotherXmppClient.Infrastructure.Events;
using YetAnotherXmppClient.Infrastructure.Queries;

//XEP-0085: Chat State Notifications

namespace YetAnotherXmppClient.Protocol.Handler
{
    public enum ChatState
    {
        active,
        inactive,
        gone,
        composing,
        paused,
    }

    internal sealed class ChatStateNotificationsProtocolHandler : ProtocolHandlerBase, IMessageReceivedCallback, IOutgoingMessageCallback, IAsyncCommandHandler<SendChatStateNotificationCommand>
    {
        private static readonly TimeSpan GoInactiveDelay = TimeSpan.FromSeconds(30);

        private ConcurrentDictionary<string, CancellationTokenSource> goInactiveCancellationTokenSources = new ConcurrentDictionary<string, CancellationTokenSource>();

        public ChatStateNotificationsProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters, IMediator mediator)
            : base(xmppStream, runtimeParameters, mediator)
        {
            this.XmppStream.RegisterMessageCallback(this);
            this.XmppStream.RegisterOutgoingMessageCallback(this);
            this.Mediator.RegisterHandler<SendChatStateNotificationCommand>(this);
            this.Mediator.Execute(new RegisterFeatureCommand(ProtocolNamespaces.ChatStateNotifications));
        }

        void IOutgoingMessageCallback.HandleOutgoingMessage(ref Message message)
        {
            // do not add chat state to messages of other type than 'chat'
            if (!message.Type.HasValue || message.Type.Value != MessageType.chat)
                return;

            // do not add chat state if the user does not wish to
            if (!(bool)this.Mediator.Query<GetPreferenceValueQuery, object>(new GetPreferenceValueQuery("SendChatStateNotifications", true)))
                return;

            // adding active chat state to chat message if it does not already contains a chat state
            if (ExtractChatState(message).HasValue)
                return;

            // cancel the go-inactive-task
            this.CancelInactiveTransistion(message.To);

            message = message.CloneAndAddElement(CreateXElementFromState(ChatState.active));
        }

        Task IMessageReceivedCallback.HandleMessageReceivedAsync(Message message)
        {
            ChatState? state = ExtractChatState(message);

            if (state.HasValue)
            {
                var @event = new ChatStateNotificationReceivedEvent
                                {
                                    FullJid = message.From,
                                    State = state.Value,
                                    Thread = message.Thread
                                };
                return this.Mediator.PublishAsync(@event);
            }

            return Task.CompletedTask;
        }

        private static ChatState? ExtractChatState(Message message)
        {
            if (message.HasElement(XNames.chatstates_active))
            {
                return ChatState.active;
            }
            else if (message.HasElement(XNames.chatstates_composing))
            {
                return ChatState.composing;
            }
            else if (message.HasElement(XNames.chatstates_paused))
            {
                return ChatState.paused;
            }
            else if (message.HasElement(XNames.chatstates_inactive))
            {
                return ChatState.inactive;
            }
            else if (message.HasElement(XNames.chatstates_gone))
            {
                return ChatState.gone;
            }

            return null;
        }

        public Task SendStandaloneChatStateMessageAsync(string fullJid, ChatState state, string thread = null)
        {
            if (!(bool)this.Mediator.Query<GetPreferenceValueQuery, object>(new GetPreferenceValueQuery("SendChatStateNotifications", true)))
                return Task.CompletedTask;

            if (state == ChatState.paused)
            {   // if pausing then go into inactive state after some time
                var cts = this.goInactiveCancellationTokenSources.AddOrUpdate(fullJid, _ => new CancellationTokenSource(), (_, existingCts) =>
                    {
                        existingCts.Cancel(false);
                        return new CancellationTokenSource();
                    });
                Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(GoInactiveDelay, cts.Token).ConfigureAwait(false);

                            await this.SendStandaloneChatStateMessageAsync(fullJid, ChatState.inactive, thread).ConfigureAwait(false);
                        }
                        catch
                        {
                            // going inactive has been canceled
                        }
                        finally
                        {
                            cts.Dispose();
                            this.goInactiveCancellationTokenSources.TryRemove(fullJid, out _);
                        }
                    });
            }
            else if (state != ChatState.inactive)
            { // going to any chat state other than inactive should cancel the go-inactive-task
                this.CancelInactiveTransistion(fullJid);
            }

            var message = new Message(CreateXElementFromState(state), thread == null ? null : new XElement("thread", thread))
                              {
                                  From = this.RuntimeParameters["jid"],
                                  To = fullJid,
                                  Type = MessageType.chat
                              };
            return this.XmppStream.WriteElementAsync(message);
        }

        private static XElement CreateXElementFromState(ChatState state)
        {
            switch (state)
            {
                case ChatState.active:
                    return new XElement(XNames.chatstates_active);
                case ChatState.inactive:
                    return new XElement(XNames.chatstates_inactive);
                case ChatState.gone:
                    return new XElement(XNames.chatstates_gone);
                case ChatState.composing:
                    return new XElement(XNames.chatstates_composing);
                case ChatState.paused:
                    return new XElement(XNames.chatstates_paused);
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        Task IAsyncCommandHandler<SendChatStateNotificationCommand>.HandleCommandAsync(SendChatStateNotificationCommand command)
        {
            return this.SendStandaloneChatStateMessageAsync(command.FullJid, command.State, command.Thread);
        }

        private void CancelInactiveTransistion(string fullJid)
        {
            if (this.goInactiveCancellationTokenSources.TryRemove(fullJid, out var cts))
            {
                cts.Cancel(false);
            }
        }
    }
}
