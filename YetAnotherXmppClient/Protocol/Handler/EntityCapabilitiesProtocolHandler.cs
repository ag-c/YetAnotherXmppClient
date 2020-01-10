using System.Collections.Generic;
using System.Threading.Tasks;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Protocol.Handler.ServiceDiscovery;

//XEP-0115: Entity Capabilities

namespace YetAnotherXmppClient.Protocol.Handler
{
    class EntityCapabilitiesProtocolHandler : ProtocolHandlerBase, IPresenceReceivedCallback
    {
        private readonly ServiceDiscoveryProtocolHandler serviceDiscoveryProtocolHandler;

        //<verification-string, capabilities>
        private readonly Dictionary<string, EntityInfo> capabilitiesByVer = new Dictionary<string, EntityInfo>();
        //<full-jid, capbiltities>
        private readonly Dictionary<string, EntityInfo> capabilitiesByFullJid = new Dictionary<string, EntityInfo>();

        public EntityCapabilitiesProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters, IMediator mediator, ServiceDiscoveryProtocolHandler serviceDiscoveryProtocolHandler)
            : base(xmppStream, runtimeParameters, mediator)
        {
            this.serviceDiscoveryProtocolHandler = serviceDiscoveryProtocolHandler;
            this.XmppStream.RegisterPresenceContentCallback(XNames.caps_c, this);
        }

        async Task IPresenceReceivedCallback.HandlePresenceReceivedAsync(Core.Stanza.Presence presence)
        {
            var cElem = presence.Element(XNames.caps_c);
            var node = cElem.Attribute("node").Value;
            var ver = cElem.Attribute("ver").Value;

            // do we need to query the capabilities for this verification string?
            if (!this.capabilitiesByVer.ContainsKey(ver))
            {
                var entityInfo = await this.serviceDiscoveryProtocolHandler.QueryEntityInformationAsync(presence.From, $"{node}#{ver}").ConfigureAwait(false);
                this.capabilitiesByVer.Add(ver, entityInfo);
            }

            this.capabilitiesByFullJid.Add(presence.From, this.capabilitiesByVer[ver]);

            //UNDONE publish event
        }

        public EntityInfo GetCapabilities(string fullJid)
        {
            if(this.capabilitiesByFullJid.TryGetValue(fullJid, out var capabilities))
            {
                return capabilities;
            }

            return null;
        }
    }
}
