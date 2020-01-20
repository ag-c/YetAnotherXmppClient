using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Core.StanzaParts;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Commands;

namespace YetAnotherXmppClient.Protocol.Handler
{
    //XEP-0092
    internal sealed class SoftwareVersionProtocolHandler : ProtocolHandlerBase, IIqReceivedCallback
    {
        public SoftwareVersionProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters, IMediator mediator)
            : base(xmppStream, runtimeParameters, mediator)
        {
            this.XmppStream.RegisterIqNamespaceCallback(XNamespaces.version, this);
            this.Mediator.Execute(new RegisterFeatureCommand(ProtocolNamespaces.SoftwareVersion));
        }

        async Task IIqReceivedCallback.HandleIqReceivedAsync(Iq iq)
        {
            if (!iq.HasElement(XNames.version_query))
            {
                return;
            }

            var responseIq = iq.CreateResultResponse(
                content: new VersionQuery("YetAnotherXmppClient", "0.0.1", "TODO"),
                from: this.RuntimeParameters["jid"]);

            await this.XmppStream.WriteElementAsync(responseIq).ConfigureAwait(false);
        }
    }
}
