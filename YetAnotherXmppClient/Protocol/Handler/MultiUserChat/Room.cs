using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using YetAnotherXmppClient.Core.StanzaParts;
using YetAnotherXmppClient.Extensions;

namespace YetAnotherXmppClient.Protocol.Handler.MultiUserChat
{
    public class Occupant
    {
        public string Nickname { get; }
        public string FullJid { get; }
        public Affiliation Affiliation { get; }
        public Role Role { get; }
        public PresenceShow Show { get; internal set; }
        public string Status { get; internal set; }

        internal Occupant(string nickname, string fullJid, Affiliation affiliation, Role role)
        {
            this.Nickname = nickname;
            this.FullJid = fullJid;
            this.Affiliation = affiliation;
            this.Role = role;
        }
    }

    public enum OccupantUpdateCause
    {
        Added,
        Changed,
        Removed
    }

    public class Room
    {
        private readonly MultiUserChatProtocolHandler protocolHandler;

        // <nickname, Occupant>
        private readonly ConcurrentDictionary<string, Occupant> occupants = new ConcurrentDictionary<string, Occupant>();
        private string errorText;

        public string Jid { get; }
        public string Name { get; } //UNDONE 

        private string subject;
        public string Subject
        {
            get => this.subject;
            internal set
            {
                this.subject = value;
                this.SubjectChanged?.Invoke(this, value);
            }
        }

        public RoomType Type { get; internal set; }

        public bool IsLogging { get; internal set; }

        public Occupant Self { get; private set; }
        public IEnumerable<Occupant> Occupants => this.occupants.Values;

        //UNDONE TypeUpdated event?

        public event EventHandler<Occupant> SelfUpdated;
        public event EventHandler<(Occupant Occupant, OccupantUpdateCause Cause)> OccupantsUpdated;

        public event EventHandler<string> SubjectChanged; 

        private event EventHandler<string> errorOccurred;
        public event EventHandler<string> ErrorOccurred
        {
            add
            {
                this.errorOccurred += value;
                if(this.errorText != null)
                    value?.Invoke(this, this.errorText);
            }
            remove => this.errorOccurred -= value;
        }

        internal Room(MultiUserChatProtocolHandler protocolHandler, string jid)
        {
            this.protocolHandler = protocolHandler;
            this.Jid = jid;
        }

        internal void AddOrUpdateOccupant(string nickname, string fullJid, Affiliation affiliation, Role role)
        {
            var cause = OccupantUpdateCause.Added;
            var occupant = this.occupants.AddOrUpdate(nickname, _ => new Occupant(nickname, fullJid, affiliation, role), (_, existing) =>
                {
                    cause = OccupantUpdateCause.Changed;
                    return new Occupant(nickname, fullJid, affiliation, role);
                });
            this.OccupantsUpdated?.Invoke(this, (occupant, cause));

            if (fullJid != null) //UNDONE not really needed as advertised with status-code-100?!
            {
                this.Type = RoomType.NonAnonymous;
            }
        }

        internal void RemoveOccupant(string nickname)
        {
            if (this.occupants.TryRemove(nickname, out var removedOccupant))
            {
                this.OccupantsUpdated?.Invoke(this, (removedOccupant, OccupantUpdateCause.Removed));
            }
        }

        internal void SetSelf(string nickname, string fullJid, Affiliation affiliation, Role role)
        {
            this.Self = new Occupant(nickname, fullJid, affiliation, role);
            this.SelfUpdated?.Invoke(this, this.Self);
        }

        internal void UpdateOccupantsShow(string nickname, PresenceShow show)
        {
            this.UpdateOccupant(nickname, occupant => occupant.Show = show);
        }

        internal void UpdateOccupantsStatus(string nickname, string status)
        {
            this.UpdateOccupant(nickname, occupant => occupant.Status = status);
        }

        private void UpdateOccupant(string nickname, Action<Occupant> updateAction)
        {
            if (this.occupants.TryGetValue(nickname, out var occupant))
            {
                updateAction(occupant);
            }
        }

        internal void OnError(string errorText)
        {
            this.errorText = errorText;
            this.errorOccurred?.Invoke(this, errorText);
        }

        public Task ChangeAvailabilityAsync(PresenceShow show, string status = null)
        {
            return this.protocolHandler.ChangeAvailabilityAsync(this.Jid, show, status);
        }

        public Task ExitAsync()
        {
            return this.protocolHandler.ExitRoomAsync(this.Jid);
        }

        public Task ChangeSubjectAsync(string subject)
        {
            return this.protocolHandler.ChangeRoomSubjectAsync(this.Jid, subject);
        }

        public Task<bool> KickOccupantAsync(string nickname, string reason = null)
        {
            return this.protocolHandler.KickRoomOccupantAsync(this.Jid, nickname, reason);
        }

        public Task<bool> GrantOccupantVoiceAsync(string nickname, string reason = null)
        {
            return this.protocolHandler.GrantRoomOccupantVoiceAsync(this.Jid, nickname, reason);
        }

        public Task<bool> RevokeOccupantVoiceAsync(string nickname, string reason = null)
        {
            return this.protocolHandler.RevokeRoomOccupantVoiceAsync(this.Jid, nickname, reason);
        }        
        
        public Task<bool> BanOccupantAsync(string nickname, string reason = null)
        {
            if (!this.occupants.TryGetValue(nickname, out var occupant))
            {
                return Task.FromResult(false);
            }

            return this.protocolHandler.ChangeRoomOccupantAffiliationAsync(this.Jid, occupant.FullJid.ToBareJid(), Affiliation.Outcast, reason);
        }

        public Task<bool> GrantMembershipAsync(string nickname, string reason = null)
        {
            if (!this.occupants.TryGetValue(nickname, out var occupant))
            {
                return Task.FromResult(false);
            }

            return this.protocolHandler.ChangeRoomOccupantAffiliationAsync(this.Jid, occupant.FullJid.ToBareJid(), Affiliation.Member, reason);
        }

        public Task<bool> RevokeMembershipAsync(string nickname, string reason = null)
        {
            if (!this.occupants.TryGetValue(nickname, out var occupant))
            {
                return Task.FromResult(false);
            }

            return this.protocolHandler.ChangeRoomOccupantAffiliationAsync(this.Jid, occupant.FullJid.ToBareJid(), Affiliation.None, reason);
        }

        public Task<bool> GrantModeratorStatusAsync(string nickname, string reason = null)
        {
            if (!this.occupants.ContainsKey(nickname))
            {
                return Task.FromResult(false);
            }

            return this.protocolHandler.ChangeRoomOccupantRoleAsync(this.Jid, nickname, Role.Moderator, reason);
        }

        public Task<bool> RevokeModeratorStatusAsync(string nickname, string reason = null)
        {
            if (!this.occupants.ContainsKey(nickname))
            {
                return Task.FromResult(false);
            }

            return this.protocolHandler.ChangeRoomOccupantRoleAsync(this.Jid, nickname, Role.Participant, reason);
        }
    }
}