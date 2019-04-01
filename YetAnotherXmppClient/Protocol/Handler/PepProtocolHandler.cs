using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Core.StanzaParts;
using YetAnotherXmppClient.Extensions;

namespace YetAnotherXmppClient.Protocol.Handler
{
    class PepProtocolHandler : ProtocolHandlerBase
    {
        public PepProtocolHandler(AsyncXmppStream xmppStream, Dictionary<string, string> runtimeParameters)
            : base(xmppStream, runtimeParameters)
        {
        }

        //XEP-0163/6.1
        public async Task<bool> DetermineSupportAsync()
        {
            var iq = new Iq(IqType.get, new XElement(XNames.discoinfo_query))
            {
                From = this.RuntimeParameters["jid"],
                To = this.RuntimeParameters["jid"].ToBareJid()
            };

            var iqResp = await this.XmppStream.WriteIqAndReadReponseAsync(iq);

            var pepSupported = iqResp.Element(XNames.discoinfo_query).Elements(XNames.discoinfo_identity)
                .Any(idt => idt.Attribute("category")?.Value == "pubsub" &&
                            idt.Attribute("type")?.Value == "pep");

            return pepSupported;
        }

        public async Task PublishEventAsync(XElement content)
        {
            var nodeId = Guid.NewGuid().ToString();
            var itemId = (string)null;
            var iq = new Iq(IqType.set, new PubSubPublish(nodeId, itemId, content));
        }

        public async Task SubscribeToNodeAsync(string nodeId)
        {
            var iq = new Iq(IqType.set, new PubSubSubscribe(nodeId, this.RuntimeParameters["jid"].ToBareJid()))
            {
                From = this.RuntimeParameters["jid"],
                To = this.RuntimeParameters["jid"].ToBareJid() //UNDONE only server?
            };

            var iqResp = await this.XmppStream.WriteIqAndReadReponseAsync(iq);
        }
    }
}
