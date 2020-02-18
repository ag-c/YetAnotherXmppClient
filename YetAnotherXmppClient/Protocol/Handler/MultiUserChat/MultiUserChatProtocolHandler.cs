

// XEP-0045: Multi-User Chat

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Serilog;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Queries;
using YetAnotherXmppClient.Protocol.Handler.ServiceDiscovery;

namespace YetAnotherXmppClient.Protocol.Handler.MultiUserChat
{
    internal class MultiUserChatProtocolHandler : ProtocolHandlerBase, IPresenceReceivedCallback, IMessageReceivedCallback
    {
        public MultiUserChatProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters, IMediator mediator)
            : base(xmppStream, runtimeParameters, mediator)
        {
            this.XmppStream.RegisterPresenceContentCallback(XNames.mucuser_x, this);
            this.XmppStream.RegisterMessageCallback(this);
        }

        public async Task<IEnumerable<string>> DiscoverServersAsync()
        {
            var entityInfo = await this.Mediator.QueryEntityInformationTreeAsync();
            return new[] { entityInfo }.Concat(entityInfo.Children).Where(ei => ei.Features.Any(f => f.Var == ProtocolNamespaces.MultiUserChat)).Select(ei => ei.Jid);
        }

        public async Task<IEnumerable<(string Jid, string Name)>> DiscoverRoomsAsync(string serviceUrl)
        {
            var supportsMuc = await this.Mediator.QueryEntitySupportsFeatureAsync(serviceUrl, ProtocolNamespaces.MultiUserChat).ConfigureAwait(false);
            if (!supportsMuc)
            {
                return null;
            }

            var items = await this.Mediator.QueryEntityItemsAsync(serviceUrl).ConfigureAwait(false);

            return items.Select(itm => (itm.Jid, itm.Name));
        }

        public async Task<RoomInfo> QueryRoomInformationAsync(string roomJid)
        {
            var _jid = new Jid(roomJid);
            var serverSupportsMuc = await this.Mediator.QueryEntitySupportsFeatureAsync(_jid.Server, ProtocolNamespaces.MultiUserChat).ConfigureAwait(false);
            if (!serverSupportsMuc)
            {
                return null;
            }

            var entityInfo = await this.Mediator.QueryEntityInformationAsync(roomJid).ConfigureAwait(false);

            return new RoomInfo(entityInfo);
        }

        /// <summary>
        /// An implementation MAY return a list of existing occupants if that information is publicly available, or return no list at all if this information is kept private.
        /// </summary>
        /// <param name="roomJid"></param>
        public async Task<IEnumerable<Item>> QueryRoomItemsAsync(string roomJid)
        {
            var _jid = new Jid(roomJid);
            var serverSupportsMuc = await this.Mediator.QueryEntitySupportsFeatureAsync(_jid.Server, ProtocolNamespaces.MultiUserChat).ConfigureAwait(false);
            if (!serverSupportsMuc)
            {
                return null;
            }

            return await this.Mediator.QueryEntityItemsAsync(roomJid).ConfigureAwait(false);
        }


        public async Task<bool> DiscoverClientSupportAsync(Jid contactJid)
        {
            if (!contactJid.IsFull)
                throw new ArgumentException("Full jid expected!");

            return await this.Mediator.QueryEntitySupportsFeatureAsync(contactJid, ProtocolNamespaces.MultiUserChat).ConfigureAwait(false);
        }

        public async Task<IEnumerable<string>> QueryContactsCurrentRoomsAsync(Jid contactJid)
        {
            if (!contactJid.IsFull)
                throw new ArgumentException("Full jid expected!");

            var items = await this.Mediator.QueryEntityItemsAsync(contactJid, "http://jabber.org/protocol/muc#rooms").ConfigureAwait(false);
            return items.Select(itm => itm.Jid);
        }

        // <bare-jid, room>
        private readonly Dictionary<string, Room> rooms = new Dictionary<string, Room>();

        public async Task<Room> EnterRoomAsync(string roomname, string server, string nickname, string password = null, HistoryLimits historyLimits = null)
        {
            var jid = new Jid(roomname, server, nickname);
            if (!jid.IsFull)
                throw new ArgumentException("Couldn't construct full jid 'roomname@server/nickname' with given parameters!");

            if (!this.rooms.TryGetValue(jid.Bare, out var room))
            {
                room = new Room(jid.Bare);
                this.rooms.Add(jid.Bare, room);
            }

            if (historyLimits?.Since.HasValue ?? false) //TEMP
            {
                Log.Error("historyLimits.Since has value handling is not implemented!");
            }

            var historyElem = historyLimits == null ? null : new XElement(XNames.muc_history,
                                  historyLimits.MaxChars.HasValue ? new XAttribute("maxchars", historyLimits.MaxChars.Value.ToString()) : null,
                                  historyLimits.MaxStanzas.HasValue ? new XAttribute("maxstanzas", historyLimits.MaxStanzas.Value.ToString()) : null,
                                  historyLimits.Seconds.HasValue ? new XAttribute("seconds", historyLimits.Seconds.Value.ToString()) : null);

            var presence = new Core.Stanza.Presence(new XElement(XNames.muc_x, password == null ? null : new XElement(XNames.muc_password, password), historyElem))
                               {
                                   From = this.RuntimeParameters["jid"],
                                   To = jid
                               };
            await this.XmppStream.WriteElementAsync(presence).ConfigureAwait(false);

            return room;
        }

        internal async Task ChangeNickName(string roomJid, string nickname)
        {
            if(string.IsNullOrWhiteSpace(nickname))
                throw new ArgumentException("Nickname cannot be null or whitespace");

            if(!this.rooms.ContainsKey(roomJid))
                throw new InvalidOperationException($"Not an occupant of room {roomJid}");

            var presence = new Core.Stanza.Presence
                               {
                                   From = this.RuntimeParameters["jid"],
                                   To = roomJid + "/" + nickname
                               };
            await this.XmppStream.WriteElementAsync(presence);
            //UNDONE response
        }

        private async Task SendMessageToAllOccupantsAsync(string roomJid, string text)
        {
            if(!roomJid.IsBareJid())
                throw new ArgumentException("Expected bare jid as room parameter!");

            if(!this.rooms.ContainsKey(roomJid))
                throw new InvalidOperationException("Not entered in room!");

            var message = new Message(text, null)
                              {
                                  From = this.RuntimeParameters["jid"],
                                  To = roomJid,
                                  Type = MessageType.groupchat,
                                  Id = Guid.NewGuid().ToString()
                              };
            await this.XmppStream.WriteElementAsync(message).ConfigureAwait(false);
            //UNDONE error response handling
            //"If the sender is a visitor (i.e., does not have voice in a moderated room), the service MUST return a <forbidden/> error
            // to the sender and MUST NOT reflect the message to all occupants. If the sender is not an occupant of the room, the
            // service SHOULD return a <not-acceptable/> error"
        }

        Task IPresenceReceivedCallback.HandlePresenceReceivedAsync(Core.Stanza.Presence presence)
        {
            var occupantJid = new Jid(presence.From);

            if (!this.rooms.TryGetValue(occupantJid.Bare, out var room))
            {
                Log.Error("Received MUC presence from unknown room!");
                return Task.CompletedTask;
            }

            var xElem = presence.Element(XNames.mucuser_x);
            if (xElem.IsEmpty)
            {
                var errorElem = presence.Element("error");
                Expectation.Expect(() => errorElem != null, presence, "");
                room.OnError(errorElem.FirstElement().Value); //UNDONE create meaningful errortext or object
                return Task.CompletedTask;
            }

            var itemElem = xElem.Element(XNames.mucuser_item);
            var affiliation = Enum.Parse<Affiliation>(itemElem.Attribute("affiliation").Value, ignoreCase: true);
            var role = Enum.Parse<Role>(itemElem.Attribute("role").Value, ignoreCase: true);
            var fullJid = itemElem.Attribute("jid")?.Value;

            var statusElems = xElem.Elements(XNames.mucuser_status);

            //"the "self-presence" sent by the room to the new user MUST include a status code of 110"
            if (statusElems.AnyWithAttributeValue("code", StatusCodes.SelfPresence))
            {
                //UNDONE what to do with the "self-presence"?
                //"This self-presence MUST NOT be sent to the new occupant until the room has sent the presence of all other occupants
                // to the new occupant; this enables the new occupant to know when it has finished receiving the room roster."
                //"The service MAY rewrite the new occupant's roomnick (e.g., if roomnicks are locked down or based on some other policy)."
                room.SetSelf(occupantJid.Resource, presence.To, affiliation, role);
                return Task.CompletedTask;
            }
            if (statusElems.AnyWithAttributeValue("code", StatusCodes.NonAnonymous))
            {
                room.Type = RoomType.NonAnonymous;
            }
            if (statusElems.AnyWithAttributeValue("code", StatusCodes.Logging))
            {
                room.IsLogging = true;
            }

            room.AddOrUpdateOccupant(occupantJid.Resource, fullJid, affiliation, role);

            return Task.CompletedTask;
        }

        Task IMessageReceivedCallback.HandleMessageReceivedAsync(Message message)
        {
            if (message.Type != MessageType.groupchat)
                return Task.CompletedTask;

            var fromJid = new Jid(message.From);

            if (this.rooms.TryGetValue(fromJid.Bare, out var room))
            {
                var subjectElem = message.Element("subject");
                if (subjectElem != null && !message.HasElement("body"))
                {
                    //"only a message that contains a <subject/> but no <body/> element shall be considered a subject change for MUC purposes."
                    room.Subject = subjectElem.Value;
                    return Task.CompletedTask;
                }

                var delayElem = message.Element(XNames.delay_delay);
                if (delayElem != null)
                {
                    //"Discussion history messages MUST be stamped with Delayed Delivery (XEP-0203) [14] information qualified by
                    // the 'urn:xmpp:delay' namespace to indicate that they are sent with delayed delivery and to specify the times
                    // at which they were originally sent. The 'from' attribute MUST be set to the JID of the room itself."

                    //TODO
                }
                //UNDONE room.AddMessage(..);
            }

            return Task.CompletedTask;
        }

        private static class StatusCodes
        {
            public const string NonAnonymous = "100";
            public const string SelfPresence = "110";
            public const string Logging = "170";
        }
    }
}
