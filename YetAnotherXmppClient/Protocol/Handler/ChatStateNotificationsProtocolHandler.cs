using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Infrastructure;
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

    class ChatStateNotificationsProtocolHandler : ProtocolHandlerBase, IMessageReceivedCallback, IAsyncCommandHandler<SendChatStateNotificationCommand>
    {
        public ChatStateNotificationsProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters, IMediator mediator)
            : base(xmppStream, runtimeParameters, mediator)
        {
            this.XmppStream.RegisterMessageCallback(this);
            this.Mediator.RegisterHandler<SendChatStateNotificationCommand>(this);
        }

        public Task MessageReceivedAsync(Message message)
        {
            ChatState? state = null;

            if (message.HasElement(XNames.chatstates_active))
            {
                state = ChatState.active;
            }
            else if (message.HasElement(XNames.chatstates_composing))
            {
                state = ChatState.composing;
            }
            else if (message.HasElement(XNames.chatstates_paused))
            {
                state = ChatState.paused;
            }
            else if (message.HasElement(XNames.chatstates_inactive))
            {
                state = ChatState.inactive;
            }
            else if (message.HasElement(XNames.chatstates_gone))
            {
                state = ChatState.gone;
            }

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

        public Task SendStandaloneChatStateMessageAsync(string fullJid, ChatState state, string thread = null)
        {
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
    }
}
