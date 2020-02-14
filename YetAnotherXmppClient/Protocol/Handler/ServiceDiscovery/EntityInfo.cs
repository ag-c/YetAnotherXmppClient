using System.Collections.Generic;

namespace YetAnotherXmppClient.Protocol.Handler.ServiceDiscovery
{
    public class EntityInfo
    {
        public string Jid { get; set; }

        public IEnumerable<Identity> Identities { get; set; }
        public IEnumerable<Feature> Features { get; set; }

        public IEnumerable<EntityInfo> Children { get; set; }
    }
}