using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using static YetAnotherXmppClient.Expectation;

namespace YetAnotherXmppClient.Protocol
{
    public interface IServerIqCallback
    {
        void IqReceived(XElement xElem);
    }

    public interface IMessageStanzaCallback
    {
        void MessageReceived(XElement messageElem);
    }

    public class ImProtocolHandler : IMessageStanzaCallback//: /*ProtocolHandlerBase,*/
    {
        private readonly AsyncXmppStream xmppServerStream;
        private readonly Dictionary<string, string> runtimeParameters;

        public ImProtocolHandler(AsyncXmppStream xmppStream/*Stream serverStream*/, Dictionary<string, string> runtimeParameters)
            //: base(serverStream)
        {
            this.xmppServerStream = xmppStream;
            this.runtimeParameters = runtimeParameters;

            this.xmppServerStream.RegisterMessageCallback(this);
        }

        public Action<Jid, string> OnMessageReceived { get; set; }

        //rfc3921
        public async Task EstablishSessionAsync()
        {
            var iq = new Iq(IqType.set, new XElement(XNames.session_session))
            {
                Id = Guid.NewGuid().ToString()
            };

//            await this.textWriter.WriteAndFlushAsync(iq);

//            var iqResp = await ReadIqStanzaAsync();
            var iqResp = await this.xmppServerStream.WriteIqAndReadReponseAsync(iq);
            Expect("result", iqResp.Attribute("type").Value);
            Expect(iq.Id, iqResp.Attribute("id").Value);
        }

        public async Task SendMessage(string recipient, string message)
        {
            var messageElem = new XElement("message",
                new XAttribute("to", recipient),
                new XAttribute("from", this.runtimeParameters["jid"]),
                new XAttribute("type", "chat"),
                new XElement("body", message));

            await this.xmppServerStream.WriteAsync(messageElem.ToString());
        }


        void IMessageStanzaCallback.MessageReceived(XElement messageElem)
        {
            Expect("message", messageElem.Name, messageElem);

            var sender = messageElem.Attribute("from").Value;
            var text = messageElem.Element("body").Value;

            this.OnMessageReceived?.Invoke(new Jid(sender), text);
        }
    }

    static class StringExtensions
    {
        public static string ToBareJid(this string jid)
        {
            if (jid.Contains("/"))
            {
                return jid.Substring(0, jid.IndexOf('/') + 1);
            }

            return jid;
        }
    }


    static class XElementExtensions2
    {
        public static bool HasErrorType(this XElement xElem)
        {
            return xElem.Attribute("type")?.Value == "error";
        }

        public static bool HasAttribute(this XElement xElem, string name)
        {
            return xElem.Attribute(name) != null;
        }
    }
}