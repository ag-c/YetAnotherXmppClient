using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Serilog;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Extensions;
using static YetAnotherXmppClient.Expectation;

namespace YetAnotherXmppClient.Protocol
{
    interface IServerIqCallback
    {
        void IqReceived(XElement xElem);
    }

    class ImProtocolHandler : /*ProtocolHandlerBase,*/
    {
        private readonly XmppStream xmppServerStream;
        private readonly Dictionary<string, string> runtimeParameters;

        public ImProtocolHandler(XmppStream xmppStream/*Stream serverStream*/, Dictionary<string, string> runtimeParameters)
            //: base(serverStream)
        {
            this.xmppServerStream = xmppStream;
            this.runtimeParameters = runtimeParameters;
            
            
        }

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