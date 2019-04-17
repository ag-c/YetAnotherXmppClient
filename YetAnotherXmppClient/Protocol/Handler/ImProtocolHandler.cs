using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using Serilog;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Extensions;
using static YetAnotherXmppClient.Expectation;

namespace YetAnotherXmppClient.Protocol.Handler
{
    public class ChatSession : IEquatable<ChatSession>
    {
        private readonly Func<string, Task> sendMessageAction;

        public string Thread { get; }
        public string OtherJid { get; set; }
        public List<string> Messages { get; } = new List<string>(); //UNDONE

        public ChatSession(string thread, string otherJid, /*Func<string, string, string, Task>*/Func<string, Task> sendMessageAction)
        {
            this.Thread = thread ?? throw new ArgumentNullException(nameof(thread));
            this.OtherJid = otherJid ?? throw new ArgumentNullException(nameof(otherJid));
            this.sendMessageAction = sendMessageAction;
        }

        public Task SendMessageAsync(string message)
        {
            this.Messages.Add("Me: " + message);
            return this.sendMessageAction(message);
        }

        public bool Equals(ChatSession other)
        {
            return this.Thread == other?.Thread;
        }
    }

    public class ImProtocolHandler : ProtocolHandlerBase, IMessageReceivedCallback
    {
        //<threadid, chatdata>
        private readonly ConcurrentDictionary<string, ChatSession> chatSessions = new ConcurrentDictionary<string, ChatSession>();

        public ImProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters)
            : base(xmppStream, runtimeParameters)
        {
            this.XmppStream.RegisterMessageCallback(this);
        }

        public Action<ChatSession, string> MessageReceived { get; set; }


        public async Task SendMessageAsync(string recipientJid, string message, string thread=null)
        {
            var messageElem = new Message(message, thread)
            {
                From = this.RuntimeParameters["jid"],
                To = recipientJid.ToBareJid(),
                Type = "chat"
            };

            await this.XmppStream.WriteElementAsync(messageElem);
        }


        void IMessageReceivedCallback.MessageReceived(Message message)
        {
            Expect("message", message.Name.LocalName, message);

            var xml = message.ToString();
            var sender = message.From;
            var text = message.Element("{jabber:client}body").Value;

            ChatSession chatSession = null;
            if (message.Thread != null)
            {
                chatSession = this.chatSessions.GetOrAdd(message.Thread, 
                    thread => new ChatSession(message.Thread, sender, 
                        msg => this.SendMessageAsync(sender, msg, message.Thread)));
                chatSession.OtherJid = sender; // take over full jid of sender
                chatSession.Messages.Add(text);
            }
            else
            {
                Log.Debug("Received message without thread id");
            }

            this.MessageReceived?.Invoke(chatSession, text);
        }

        public ChatSession StartChatSession(string fullJid)
        {
            //TODO check if session with same fullJid already exists?
            var thread = Guid.NewGuid().ToString();
            var session = new ChatSession(thread, fullJid, msg => this.SendMessageAsync(fullJid, msg, thread));
            this.chatSessions.TryAdd(thread, session);
            return session;
        }
    }
}