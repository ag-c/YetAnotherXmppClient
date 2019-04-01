using System.Collections.Generic;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Extensions;
using static YetAnotherXmppClient.Expectation;

namespace YetAnotherXmppClient.Protocol.Handler
{
    class ServiceDiscoveryProtocolHandler : ProtocolHandlerBase, IIqReceivedCallback
    {
        public ServiceDiscoveryProtocolHandler(AsyncXmppStream xmppStream, Dictionary<string, string> runtimeParameters) 
            : base(xmppStream, runtimeParameters)
        {
            this.XmppStream.RegisterIqNamespaceCallback(XNamespaces.discoinfo, this);
        }

        public async void IqReceived(Iq iq)
        {
            Expect(() => iq.HasElement(XNames.discoinfo_query), iq);

            var iqResp = new Iq(IqType.result, 
                new XElement(XNames.discoinfo_query, 
                    new XElement(XNames.discoinfo_identity, new XAttribute("category", "client"), new XAttribute("type", "pc"), new XAttribute("name", "YetAnotherXmppClient")),
                    new XElement(XNames.discoinfo_feature, new XAttribute("var", "http://jabber.org/protocol/disco#info")),
                    new XElement(XNames.discoinfo_feature, new XAttribute("var", "urn:xmpp:time")),
                    new XElement(XNames.discoinfo_feature, new XAttribute("var", "eu.siacs.conversations.axolotl.devicelist+notify"))))
            {
                Id = iq.Id,
                From = this.RuntimeParameters["jid"],
                To = iq.From
            };

            await this.XmppStream.WriteAsync(iqResp);
        }
    }
}
