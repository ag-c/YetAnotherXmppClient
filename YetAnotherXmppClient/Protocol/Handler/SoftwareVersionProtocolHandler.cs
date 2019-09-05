using System.Collections.Generic;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Extensions;

namespace YetAnotherXmppClient.Protocol.Handler
{
    //XEP-0092
    internal class SoftwareVersionProtocolHandler : ProtocolHandlerBase, IIqReceivedCallback
    {
        public SoftwareVersionProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters)
            : base(xmppStream, runtimeParameters)
        {
            this.XmppStream.RegisterIqNamespaceCallback(XNamespaces.version, this);
        }

        async void IIqReceivedCallback.IqReceived(Iq iq)
        {
            if(!iq.HasElement(XNames.version_query))
                return;

            await this.XmppStream.WriteElementAsync(new Iq(IqType.result, new XElement(XNames.version_query, 
                                                                                new XElement(XNames.version_name, "YetAnotherXmppClient"),
                                                                                new XElement(XNames.version_version, "0.0.1"),
                                                                                new XElement(XNames.version_os, "TODO")))
                                                        {
                                                            From = this.RuntimeParameters["jid"],
                                                            To = iq.From,
                                                            Id = iq.Id
                                                        });
        }
    }
}
