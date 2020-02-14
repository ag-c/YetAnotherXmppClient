

// XEP-0045: Multi-User Chat

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Queries;
using YetAnotherXmppClient.Protocol.Handler.ServiceDiscovery;

namespace YetAnotherXmppClient.Protocol.Handler.MultiUserChat
{
    internal class MultiUserChatProtocolHandler : ProtocolHandlerBase
    {
        public MultiUserChatProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters, IMediator mediator)
            : base(xmppStream, runtimeParameters, mediator)
        {
        }

        public async Task<IEnumerable<Room>> DiscoverRoomsAsync(string serviceUrl)
        {
            var supportsMuc = await this.Mediator.QueryAsync<EntitySupportsFeatureQuery, bool>(new EntitySupportsFeatureQuery(serviceUrl, ProtocolNamespaces.MultiUserChat)).ConfigureAwait(false);
            if (!supportsMuc)
            {
                return null;
            }

            var items = await this.Mediator.QueryAsync<EntityItemsQuery, IEnumerable<Item>>(new EntityItemsQuery(serviceUrl)).ConfigureAwait(false);

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

        /// <summary>
        /// An implementation MAY return a list of existing occupants if that information is publicly available, or return no list at all if this information is kept private.
        /// </summary>
        /// <param name="roomJid"></param>
        public async Task<IEnumerable<Item>> QueryRoomItemsAsync(string roomJid)
        {
            var _jid = new Jid(roomJid);
            var serverSupportsMuc = await this.Mediator.QueryAsync<EntitySupportsFeatureQuery, bool>(new EntitySupportsFeatureQuery(_jid.Server, ProtocolNamespaces.MultiUserChat)).ConfigureAwait(false);
            if (!serverSupportsMuc)
            {
                return null;
            }

            return await this.Mediator.QueryAsync<EntityItemsQuery, IEnumerable<Item>>(new EntityItemsQuery(roomJid)).ConfigureAwait(false);
        }


        public async Task<bool> DiscoverClientSupportAsync(Jid contactJid)
        {
            if (!contactJid.IsFull)
                throw new ArgumentException("Full jid expected!");

            return await this.Mediator.QueryAsync<EntitySupportsFeatureQuery, bool>(new EntitySupportsFeatureQuery(contactJid, ProtocolNamespaces.MultiUserChat)).ConfigureAwait(false);
        }

        public async Task<IEnumerable<Item>> QueryContactsCurrentRoomsAsync(Jid contactJid)
        {
            if (!contactJid.IsFull)
                throw new ArgumentException("Full jid expected!");

            return await this.Mediator.QueryAsync<EntityItemsQuery, IEnumerable<Item>>(new EntityItemsQuery(contactJid, "http://jabber.org/protocol/muc#rooms")).ConfigureAwait(false);
        }
    }
}
