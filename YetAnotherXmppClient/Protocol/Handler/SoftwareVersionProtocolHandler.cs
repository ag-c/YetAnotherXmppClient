using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Core.StanzaParts;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Infrastructure;

namespace YetAnotherXmppClient.Protocol.Handler
{
    //XEP-0092
    internal class SoftwareVersionProtocolHandler : ProtocolHandlerBase, IIqReceivedCallback
    {
        public SoftwareVersionProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters, IMediator mediator)
            : base(xmppStream, runtimeParameters, mediator)
        {
            this.XmppStream.RegisterIqNamespaceCallback(XNamespaces.version, this);
        }

        async Task IIqReceivedCallback.IqReceivedAsync(Iq iq)
        {
            if (!iq.HasElement(XNames.version_query))
            {
                return;
            }

            var responseIq = iq.CreateResultResponse(
                content: new VersionQuery("YetAnotherXmppClient", "0.0.1", "TODO"),
                from: this.RuntimeParameters["jid"]);

            await this.XmppStream.WriteElementAsync(responseIq);
        }
    }
}
