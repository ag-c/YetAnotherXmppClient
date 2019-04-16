using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Extensions;
using static YetAnotherXmppClient.Expectation;

namespace YetAnotherXmppClient.Protocol.Handler
{
    public class ChatSession
    {
        public string Thread { get; set; }
        public List<string> Messages { get; } = new List<string>(); //UNDONE
    }

    public class ImProtocolHandler : ProtocolHandlerBase, IMessageReceivedCallback
    {
        //<threadid, chatdata>
        private ConcurrentDictionary<string, ChatSession> chatSessions = new ConcurrentDictionary<string, ChatSession>();

        public ImProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters)
            : base(xmppStream, runtimeParameters)
        {
            this.XmppStream.RegisterMessageCallback(this);
        }

        public Action<ChatSession, Jid, string> MessageReceived { get; set; }

        //rfc3921
        //[Obsolete]
        //public async Task EstablishSessionAsync()
        //{
        //    var iq = new Iq(IqType.set, new XElement(XNames.session_session));

        //    var iqResp = await this.xmppServerStream.WriteIqAndReadReponseAsync(iq);

        //    Expect("result", iqResp.Attribute("type").Value);
        //    Expect(iq.Id, iqResp.Attribute("id").Value);
        //}

        public async Task SendMessageAsync(string recipientJid, string message)
        {
            var messageElem = new Message(new XElement("body", message))
            {
                From = this.RuntimeParameters["jid"],
                To = recipientJid.ToBareJid(),
                Type = "chat"
            };

            await this.XmppStream.WriteElementAsync(messageElem);
        }


        void IMessageReceivedCallback.MessageReceived(Message message)
        {
            Expect("message", message.Name, message);

            var sender = message.From;
            var text = message.Element("body").Value;

            var chatSession = message.Thread != null ? this.chatSessions[message.Thread] : null;

            this.MessageReceived?.Invoke(chatSession, new Jid(sender), text);
        }
    }
}