using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Serilog;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Core.StanzaParts;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Queries.MultiUserChat;
using YetAnotherXmppClient.Protocol.Handler.ServiceDiscovery;

// XEP-0045: Multi-User Chat
// XEP-0249: Direct MUC Invitations

namespace YetAnotherXmppClient.Protocol.Handler.MultiUserChat
{
    internal class MultiUserChatProtocolHandler : ProtocolHandlerBase, IPresenceReceivedCallback, IMessageReceivedCallback, IAsyncQueryHandler<EnterRoomQuery, Room>
    {
        // <room-jid, room>
        private readonly Dictionary<string, Room> rooms = new Dictionary<string, Room>();

        public MultiUserChatProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters, IMediator mediator)
            : base(xmppStream, runtimeParameters, mediator)
        {
            this.XmppStream.RegisterPresenceContentCallback(XNames.mucuser_x, this);
            this.XmppStream.RegisterMessageCallback(this);
            this.XmppStream.RegisterExclusiveMessageContentCallback(XNames.conference_x, this);
            this.Mediator.RegisterHandler<EnterRoomQuery, Room>(this);
            this.Mediator.RegisterFeature(ProtocolNamespaces.Conference);
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

        public Task<Room> EnterRoomAsync(string roomname, string server, string nickname, string password = null, HistoryLimits historyLimits = null)
        {
            return this.EnterRoomAsync($"{roomname}@{server}", nickname, password, historyLimits);
        }

        public async Task<Room> EnterRoomAsync(string roomJid, string nickname, string password = null, HistoryLimits historyLimits = null) 
        {
            var jid = new Jid(roomJid, nickname);
            if (!jid.IsFull)
                throw new ArgumentException("Couldn't construct full jid 'roomname@server/nickname' with given parameters!");

            if (!this.rooms.TryGetValue(jid.Bare, out var room))
            {
                room = new Room(this, jid.Bare);
                this.rooms.Add(jid.Bare, room);
            }
            //UNDONE already entered room with a different nickname?

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

        public Task ExitRoomAsync(string roomJid)
        {
            if (!roomJid.IsBareJid())
                throw new ArgumentException("Expected a bare JID as room parammeter!");

            if (this.rooms.Remove(roomJid, out var room))
            {
                var presence = new Core.Stanza.Presence(PresenceType.unavailable)
                                   {
                                       From = this.RuntimeParameters["jid"],
                                       To = roomJid + "/" + room.Self.Nickname
                                   };
                return this.XmppStream.WriteElementAsync(presence);
            }

            return Task.CompletedTask;
        }

        internal async Task ChangeNicknameAsync(string roomJid, string nickname)
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

        public Task ChangeAvailabilityAsync(string roomJid, PresenceShow show, string status = null)
        {
            if(!roomJid.IsBareJid())
                throw new ArgumentException("Expected a bare JID as room parammeter!");

            if (!this.rooms.TryGetValue(roomJid, out var room))
                throw new InvalidOperationException($"Not an occupant of room {roomJid}");

            var presence = new Core.Stanza.Presence(show, status)
                               {
                                   From = this.RuntimeParameters["jid"],
                                   To = roomJid + "/" + room.Self.Nickname
                               };

            return this.XmppStream.WriteElementAsync(presence);
        }

        public Task ChangeRoomSubjectAsync(string roomJid, string subject)
        {
            if (!roomJid.IsBareJid())
                throw new ArgumentException("Expected bare jid as room parameter!");

            if (!this.rooms.ContainsKey(roomJid))
                throw new InvalidOperationException("Not entered in room!");

            //UNDONE check role AND room configuration for 'muc#roomconfig_changesubject'?

            var message = new Message(new XElement("subject", subject))
                              {
                                  From = this.RuntimeParameters["jid"],
                                  To = roomJid,
                                  Type = MessageType.groupchat
                              };

            return this.XmppStream.WriteElementAsync(message);
        }

        public async Task<bool> KickRoomOccupantAsync(string roomJid, string nickname, string reason)
        {
            if (!roomJid.IsBareJid())
                throw new ArgumentException("Expected bare jid as room parameter!");

            if (!this.rooms.ContainsKey(roomJid))
                throw new InvalidOperationException("Not entered in room!");

            //UNDONE check role?

            var iq = new Iq(IqType.set, new XElement(XNames.mucadmin_query, 
                                                new XElement(XNames.mucadmin_item, new XAttribute("nick", nickname), new XAttribute("role", Role.None.ToString().ToLower()),
                                                    reason == null ? null : new XElement(XNames.mucadmin_reason, reason))));

            var responseIq = await this.XmppStream.WriteIqAndReadReponseAsync(iq).ConfigureAwait(false);

            return responseIq.Type == IqType.result;
        }

        public Task<bool> GrantRoomOccupantVoiceAsync(string roomJid, string nickname, string reason = null)
        {
            return this.ChangeRoomOccupantRoleAsync(roomJid, nickname, Role.Participant, reason);
        }

        public Task<bool> RevokeRoomOccupantVoiceAsync(string roomJid, string nickname, string reason = null)
        {
            return this.ChangeRoomOccupantRoleAsync(roomJid, nickname, Role.Visitor, reason);
        }

        private async Task<bool> ChangeRoomOccupantRoleAsync(string roomJid, string nickname, Role newRole, string reason = null)
        {
            if (!roomJid.IsBareJid())
                throw new ArgumentException("Expected bare jid as room parameter!");

            if (!this.rooms.ContainsKey(roomJid))
                throw new InvalidOperationException("Not entered in room!");

            //UNDONE check role?

            var iq = new Iq(IqType.set, new XElement(XNames.mucadmin_query,
                new XElement(XNames.mucadmin_item, new XAttribute("nick", nickname), new XAttribute("role", newRole.ToString().ToLower()),
                    reason == null ? null : new XElement(XNames.mucadmin_reason, reason))));

            var responseIq = await this.XmppStream.WriteIqAndReadReponseAsync(iq).ConfigureAwait(false);

            return responseIq.Type == IqType.result;
        }

        internal async Task<bool> ChangeRoomOccupantAffiliationAsync(string roomJid, string bareUserJid, Affiliation affiliation, string reason = null)
        {
            if (!roomJid.IsBareJid())
                throw new ArgumentException("Expected bare jid as room parameter!");

            if (!this.rooms.ContainsKey(roomJid))
                throw new InvalidOperationException("Not entered in room!");

            //UNDONE check role for admin?

            var iq = new Iq(IqType.set, new XElement(XNames.mucadmin_query,
                new XElement(XNames.mucadmin_item, new XAttribute("jid", bareUserJid.ToBareJid()), new XAttribute("affiliation", affiliation.ToString().ToLower()),
                    reason == null ? null : new XElement(XNames.mucadmin_reason, reason))));

            var responseIq = await this.XmppStream.WriteIqAndReadReponseAsync(iq).ConfigureAwait(false);

            return responseIq.Type == IqType.result;
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

            // Unavailable presence
            if (presence.Type == PresenceType.unavailable)
            {
                room.RemoveOccupant(occupantJid.Resource);
                return Task.CompletedTask;
            }

            // Affiliation & Role & Full-JID
            var itemElem = xElem.Element(XNames.mucuser_item);
            var affiliation = Enum.Parse<Affiliation>(itemElem.Attribute("affiliation").Value, ignoreCase: true);
            var role = Enum.Parse<Role>(itemElem.Attribute("role").Value, ignoreCase: true);
            var fullJid = itemElem.Attribute("jid")?.Value;

            var mucStatusElems = xElem.Elements(XNames.mucuser_status)?.ToArray();

            //"the "self-presence" sent by the room to the new user MUST include a status code of 110"
            if (mucStatusElems.AnyWithAttributeValue("code", StatusCodes.SelfPresence))
            {
                //UNDONE
                //"This self-presence MUST NOT be sent to the new occupant until the room has sent the presence of all other occupants
                // to the new occupant; this enables the new occupant to know when it has finished receiving the room roster."
                //"The service MAY rewrite the new occupant's roomnick (e.g., if roomnicks are locked down or based on some other policy)."
                room.SetSelf(occupantJid.Resource, presence.To, affiliation, role);
                return Task.CompletedTask;
            }
            if (mucStatusElems.AnyWithAttributeValue("code", StatusCodes.NonAnonymous))
            {
                room.Type = RoomType.NonAnonymous;
            }
            if (mucStatusElems.AnyWithAttributeValue("code", StatusCodes.Logging))
            {
                room.IsLogging = true;
            }

            room.AddOrUpdateOccupant(occupantJid.Resource, fullJid, affiliation, role);

            // Show & Status
            var showElem = xElem.Element("show");
            var statusElem = xElem.Element("status");
            if (showElem != null)
            {
                room.UpdateOccupantsShow(occupantJid.Resource, Enum.TryParse<PresenceShow>(showElem.Value, true, out var show) ? show : PresenceShow.Other);
            }
            if (statusElem != null)
            {
                room.UpdateOccupantsStatus(occupantJid.Resource, statusElem.Value);
            }

            return Task.CompletedTask;
        }

        async Task IMessageReceivedCallback.HandleMessageReceivedAsync(Message message)
        {
            // Handle direct invitation as specified in XEP-0249
            var xElem = message.Element(XNames.conference_x);
            if (xElem != null)
            {
                var roomJid = xElem.Element(XNames.conference_jid).Value;
                var reason = xElem.Element(XNames.conference_reason)?.Value;
                var doEnter = await this.Mediator.QueryAsync<DirectRoomInvitationQuery, bool>(new DirectRoomInvitationQuery(roomJid, reason));
                if (doEnter)
                {
                    Log.Error("//UNDONE show nickname input window to user -> join room");
                }
                return;
            }

            if (message.Type != MessageType.groupchat)
                return;

            var fromJid = new Jid(message.From);

            if (this.rooms.TryGetValue(fromJid.Bare, out var room))
            {
                var subjectElem = message.Element("subject");
                if (subjectElem != null && !message.HasElement("body"))
                {
                    //"only a message that contains a <subject/> but no <body/> element shall be considered a subject change for MUC purposes."
                    room.Subject = subjectElem.Value;
                    return;
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
        }

        Task<Room> IAsyncQueryHandler<EnterRoomQuery, Room>.HandleQueryAsync(EnterRoomQuery query)
        {
            return this.EnterRoomAsync(query.RoomJid, query.Nickname);
        }

        private static class StatusCodes
        {
            public const string NonAnonymous = "100";
            public const string SelfPresence = "110";
            public const string Logging = "170";
            public const string NicknameModified = "210";
        }
    }
}
