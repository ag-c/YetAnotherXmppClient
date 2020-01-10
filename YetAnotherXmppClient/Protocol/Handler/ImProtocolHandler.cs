using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using Serilog;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Events;
using YetAnotherXmppClient.Infrastructure.Queries;
using static YetAnotherXmppClient.Expectation;

namespace YetAnotherXmppClient.Protocol.Handler
{
    //UNDONE move to correct namespace
    public class ChatMessage
    {
        public DateTime DateTime { get; set; }
        public string From { get; set; }
        public string Text { get; set; }
    }

    //UNDONE move to correct namespace
    public class ChatSession : IEquatable<ChatSession>
    {
        private readonly Func<string, Task> sendMessageAction;

        public string Thread { get; }
        public string OtherJid { get; internal set; }
        public List<ChatMessage> Messages { get; } = new List<ChatMessage>();

        public event EventHandler<ChatMessage> NewMessage;

        public ChatSession(string thread, string otherJid, /*Func<string, string, string, Task>*/Func<string, Task> sendMessageAction)
        {
            this.Thread = thread ?? throw new ArgumentNullException(nameof(thread));
            this.OtherJid = otherJid ?? throw new ArgumentNullException(nameof(otherJid));
            this.sendMessageAction = sendMessageAction;
        }

        public async Task SendMessageAsync(string text)
        {
            var message = this.CreateMessage("Me", text);
            this.Messages.Add(message);
            await this.sendMessageAction(text).ConfigureAwait(false);

            this.NewMessage?.Invoke(this, message);
        }

        internal void AddIncomingMessage(string text)
        {
            var message = this.CreateMessage(this.OtherJid, text);

            this.Messages.Add(this.CreateMessage(this.OtherJid, text));

            this.NewMessage?.Invoke(this, message);
        }

        private ChatMessage CreateMessage(string from, string text)
        {
            return new ChatMessage
                       {
                           DateTime = DateTime.Now,
                           From = from,
                           Text = text
                       };
        }

        public bool Equals(ChatSession other)
        {
            return this.Thread == other?.Thread;
        }
    }

    public class ImProtocolHandler : ProtocolHandlerBase, IMessageReceivedCallback, IQueryHandler<StartChatSessionQuery, ChatSession>
    {
        //<threadid, chatdata>
        private readonly ConcurrentDictionary<string, ChatSession> chatSessions = new ConcurrentDictionary<string, ChatSession>();

        public ImProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters, IMediator mediator)
            : base(xmppStream, runtimeParameters, mediator)
        {
            this.Mediator.RegisterHandler<StartChatSessionQuery, ChatSession>(this);
            this.XmppStream.RegisterMessageCallback(this);
        }


        public async Task SendMessageAsync(string recipientJid, string message, string thread=null)
        {
            var messageElem = new Message(message, thread)
            {
                From = this.RuntimeParameters["jid"],
                To = recipientJid.ToBareJid(),
                Type = MessageType.chat
            };

            await this.XmppStream.WriteElementAsync(messageElem).ConfigureAwait(false);
        }


        async Task IMessageReceivedCallback.HandleMessageReceivedAsync(Message message)
        {
            Expect("message", message.Name.LocalName, message);

            if (!message.Type.HasValue || message.Type != MessageType.chat)
            {
                Log.Debug($"TODO: handle message type '{message.Type}'");
                return;
            }

            var xml = message.ToString();
            var sender = message.From;
            var text = message.Body;

            ChatSession chatSession = null;
            if (message.Thread != null)
            {
                chatSession = this.chatSessions.GetOrAdd(message.Thread, 
                    thread => new ChatSession(message.Thread, sender, 
                        msg => this.SendMessageAsync(sender, msg, message.Thread)));
                chatSession.OtherJid = sender; // take over full jid of sender
            }
            else
            {
                Log.Debug("Received message without thread id");
                // creating a session with a new thread id
                //UNDONE search for session with same jid
                var newThread = Guid.NewGuid().ToString();
                chatSession = new ChatSession(newThread, sender, 
                    msg => this.SendMessageAsync(sender, msg, message.Thread));
                this.chatSessions.TryAdd(newThread, chatSession);
            }

            chatSession.AddIncomingMessage(text);

            await this.Mediator.PublishAsync(new MessageReceivedEvent(chatSession, text)).ConfigureAwait(false);
        }

        public ChatSession StartChatSession(string fullJid)
        {
            //TODO check if session with same fullJid already exists?
            var thread = Guid.NewGuid().ToString();
            var session = new ChatSession(thread, fullJid, msg => this.SendMessageAsync(fullJid, msg, thread));
            this.chatSessions.TryAdd(thread, session);
            return session;
        }

        ChatSession IQueryHandler<StartChatSessionQuery, ChatSession>.HandleQuery(StartChatSessionQuery query)
        {
            return this.StartChatSession(query.Jid);
        }
    }
}