using YetAnotherXmppClient.Protocol.Handler.ServiceDiscovery;

namespace YetAnotherXmppClient.Infrastructure.Queries
{
    public class EntityInformationTreeQuery : IQuery<EntityInfo>
    {
        public string Jid { get; } //null: use server jid

        public EntityInformationTreeQuery(string jid = null)
        {
            this.Jid = jid;
        }
    }
}
