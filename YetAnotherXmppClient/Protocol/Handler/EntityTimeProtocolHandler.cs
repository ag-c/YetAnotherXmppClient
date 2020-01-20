using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Commands;

// XEP-0202: Entity Time

namespace YetAnotherXmppClient.Protocol.Handler
{
    internal sealed class EntityTimeProtocolHandler : ProtocolHandlerBase, IIqReceivedCallback
    {
        public EntityTimeProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters, IMediator mediator) 
            : base(xmppStream, runtimeParameters, mediator)
        {
            this.XmppStream.RegisterIqNamespaceCallback(XNamespaces.time, this);
            this.Mediator.Execute(new RegisterFeatureCommand(ProtocolNamespaces.EntityTime));
        }

        async Task IIqReceivedCallback.HandleIqReceivedAsync(Iq iq)
        {
            var tz = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
            var tzo = tz.ToString(@"hh\:mm");
            var utc = DateTime.UtcNow.ToString(@"yyyy-mm-dd\Thh:mm:ss\Z");

            var iqResp = iq.CreateResultResponse(new XElement(XNames.time_time,
                new XElement(XNames.time_tzo, tzo),
                new XElement(XNames.time_utc, utc)));

            await this.XmppStream.WriteElementAsync(iqResp).ConfigureAwait(false);
        }
    }
}
