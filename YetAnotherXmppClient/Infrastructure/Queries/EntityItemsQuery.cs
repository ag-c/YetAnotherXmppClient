using System.Collections.Generic;
using YetAnotherXmppClient.Protocol.Handler.ServiceDiscovery;

namespace YetAnotherXmppClient.Infrastructure.Queries
{
    internal class EntityItemsQuery : IQuery<IEnumerable<Item>>
    {
        public string Jid { get; set; }
        public string Node { get; set; }

        public EntityItemsQuery(string jid, string node = null)
        {
            this.Jid = jid;
            this.Node = node;
        }
    }
}
