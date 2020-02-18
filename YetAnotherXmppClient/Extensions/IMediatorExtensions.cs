using System.Collections.Generic;
using System.Threading.Tasks;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Queries;
using YetAnotherXmppClient.Protocol.Handler.ServiceDiscovery;

namespace YetAnotherXmppClient.Extensions
{
    public static class IMediatorExtensions
    {
        public static Task<bool> QueryEntitySupportsFeatureAsync(this IMediator mediator, string jid, string protocolNamespace)
        {
            return mediator.QueryAsync<EntitySupportsFeatureQuery, bool>(new EntitySupportsFeatureQuery(jid, protocolNamespace));
        }

        public static Task<IEnumerable<Item>> QueryEntityItemsAsync(this IMediator mediator, string jid, string node = null)
        {
            return mediator.QueryAsync<EntityItemsQuery, IEnumerable<Item>>(new EntityItemsQuery(jid, node));
        }

        public static Task<EntityInfo> QueryEntityInformationAsync(this IMediator mediator, string jid)
        {
            return mediator.QueryAsync<EntityInformationQuery, EntityInfo>(new EntityInformationQuery(jid));
        }
    }
}
