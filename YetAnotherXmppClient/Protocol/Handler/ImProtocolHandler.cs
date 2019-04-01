using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Extensions;

namespace YetAnotherXmppClient.Protocol.Handler
{
    class ChatSession
    {
        public string Thread { get; set; }
        public List<string> Messages { get; } = new List<string>(); //UNDONE
    }

    public class ImProtocolHandler : ProtocolHandlerBase, IMessageReceivedCallback
    {
        //<threadid, chatdata>
        private ConcurrentDictionary<string, ChatSession> chatSessions = new ConcurrentDictionary<string, ChatSession>();

        public ImProtocolHandler(AsyncXmppStream xmppStream, Dictionary<string, string> runtimeParameters)
            : base(xmppStream, runtimeParameters)
        {
            this.XmppStream.RegisterMessageCallback(this);
        }

        public Action<Jid, string> OnMessageReceived { get; set; }

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

            await this.XmppStream.WriteAsync(messageElem.ToString());
        }


        void IMessageReceivedCallback.MessageReceived(XElement messageElem)
        {
            Expectation.Expect("message", messageElem.Name, messageElem);

            var sender = messageElem.Attribute("from").Value;
            var text = messageElem.Element("body").Value;

            this.OnMessageReceived?.Invoke(new Jid(sender), text);
        }
    }
}