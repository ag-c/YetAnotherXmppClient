using YetAnotherXmppClient.Protocol.Handler.ServiceDiscovery;

namespace YetAnotherXmppClient.Infrastructure.Queries
{
    internal class EntityInformationQuery : IQuery<EntityInfo>
    {
        public string Jid { get; }

        public EntityInformationQuery(string jid)
        {
            this.Jid = jid;
        }
    }
}
