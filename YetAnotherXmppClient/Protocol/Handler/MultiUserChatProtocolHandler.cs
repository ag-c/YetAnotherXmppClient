

// XEP-0045: Multi-User Chat

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Queries;
using YetAnotherXmppClient.Protocol.Handler.ServiceDiscovery;

namespace YetAnotherXmppClient.Protocol.Handler.MultiUserChat
{
    internal enum Role
    {
        None,
        Moderator,
        Participant,
        Visitor
    }

    internal enum Affiliation
    {
        None,
        Owner,
        Admin,
        Member,
        Outcast
    }

    internal enum RoomType
    {
        Hidden,
        MembersOnly,
        Moderated,
        NonAnonymous,
        Open,
        PasswordProtected,
        Persistent,
        Public,
        SemiAnonymous,
        Temporary,
        Unmoderated,
        Unsecured
    }

    public class Room
    {
        public string Jid { get; set; }
        public string Name { get; set; }

        public Room(string jid, string name)
        {
            this.Jid = jid;
            this.Name = name;
        }
    }

    public class RoomInfo : EntityInfo
    {
        internal RoomInfo(EntityInfo entityInfo)
        {
            this.Jid = entityInfo.Jid;
            this.Identities = entityInfo.Identities;
            this.Features = entityInfo.Features;
        }
    }

    internal class MultiUserChatProtocolHandler : ProtocolHandlerBase
    {
        public MultiUserChatProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters, IMediator mediator)
            : base(xmppStream, runtimeParameters, mediator)
        {
        }

        public async Task<IEnumerable<Room>> DiscoverRoomsAsync(string jid)
        {
            var supportsMuc = await this.Mediator.QueryAsync<EntitySupportsFeatureQuery, bool>(new EntitySupportsFeatureQuery(jid, ProtocolNamespaces.MultiUserChat)).ConfigureAwait(false);
            if (!supportsMuc)
            {
                return null;
            }

            var items = await this.Mediator.QueryAsync<EntityItemsQuery, IEnumerable<Item>>(new EntityItemsQuery(jid)).ConfigureAwait(false);

            return items.Select(itm => new Room(itm.Jid, itm.Name));
        }

        public async Task<RoomInfo> QueryRoomInformationAsync(string roomJid)
        {
            var _jid = new Jid(roomJid);
            var serverSupportsMuc = await this.Mediator.QueryAsync<EntitySupportsFeatureQuery, bool>(new EntitySupportsFeatureQuery(_jid.Server, ProtocolNamespaces.MultiUserChat)).ConfigureAwait(false);
            if (!serverSupportsMuc)
            {
                return null;
            }

            var entityInfo = await this.Mediator.QueryAsync<EntityInformationQuery, EntityInfo>(new EntityInformationQuery(roomJid)).ConfigureAwait(false);

            return new RoomInfo(entityInfo);
        }
    }
}
