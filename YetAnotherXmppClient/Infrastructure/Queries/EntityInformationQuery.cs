using YetAnotherXmppClient.Protocol.Handler.ServiceDiscovery;

namespace YetAnotherXmppClient.Infrastructure.Queries
{
    internal class EntityInformationQuery : IQuery<EntityInfo>
    {
        public string FullJid { get; }

        public EntityInformationQuery(string fullJid)
        {
            this.FullJid = fullJid;
        }
    }
}
