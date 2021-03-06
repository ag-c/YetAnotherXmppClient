﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetAnotherXmppClient.Core.StanzaParts;
using YetAnotherXmppClient.Extensions;

namespace YetAnotherXmppClient.Protocol.Handler.MultiUserChat
{
    public class Occupant : ICloneable, IEquatable<Occupant>
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

        public object Clone()
        {
            return new Occupant(this.Nickname, this.FullJid, this.Affiliation, this.Role)
                       {
                           Show = this.Show,
                           Status = this.Status
                       };
        }

        public bool Equals(Occupant other)
        {
            return this.Nickname == other?.Nickname;
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

        public string Subject { get; private set; }

        public RoomType Type { get; internal set; }

        public bool IsLogging { get; internal set; }

        public Occupant Self { get; private set; }
        public IEnumerable<Occupant> Occupants => this.occupants.Values;

        //UNDONE TypeUpdated event?

        public event EventHandler<(Occupant OldSelf, Occupant NewSelf)> SelfUpdated;
        public event EventHandler<(Occupant OldOccupant, Occupant NewOccupant, OccupantUpdateCause Cause)> OccupantsUpdated;

        public event EventHandler<(string Subject, string Nickname)> SubjectChanged;

        public event EventHandler<(string MesssageText, string Nickname, DateTime Time)> NewMessage; 

        public event EventHandler Exited; 

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
            if (this.occupants.TryGetValue(nickname, out var oldOccupant))
                oldOccupant = (Occupant)oldOccupant.Clone();

            var cause = OccupantUpdateCause.Added;
            var newOccupant = this.occupants.AddOrUpdate(nickname, _ => new Occupant(nickname, fullJid, affiliation, role), (_, existing) =>
                {
                    cause = OccupantUpdateCause.Changed;
                    return new Occupant(nickname, fullJid, affiliation, role);
                });
            this.OccupantsUpdated?.Invoke(this, (oldOccupant, newOccupant, cause));

            if (fullJid != null) //UNDONE not really needed as advertised with status-code-100?!
            {
                this.Type = RoomType.NonAnonymous;
            }
        }

        internal void RemoveOccupant(string nickname)
        {
            if (this.occupants.TryRemove(nickname, out var removedOccupant))
            {
                this.OccupantsUpdated?.Invoke(this, (removedOccupant, null, OccupantUpdateCause.Removed));
            }
        }

        internal void SetSelf(string nickname, string fullJid, Affiliation affiliation, Role role)
        {
            var oldSelf = this.Self;
            this.Self = new Occupant(nickname, fullJid, affiliation, role);
            this.SelfUpdated?.Invoke(this, (oldSelf, this.Self));
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

        public async Task ExitAsync()
        {
            await this.protocolHandler.ExitRoomAsync(this.Jid);
            this.Exited?.Invoke(this, EventArgs.Empty);
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

        public Task<bool> GrantOwnerStatusAsync(string nickname, string reason = null)
        {
            if (!this.occupants.TryGetValue(nickname, out var occupant))
            {
                return Task.FromResult(false);
            }

            return this.protocolHandler.ChangeRoomOccupantAffiliationAsync(this.Jid, occupant.FullJid.ToBareJid(), Affiliation.Owner, reason);
        }

        public Task<bool> RevokeOwnerStatusAsync(string nickname, string reason = null)
        {
            if (!this.occupants.TryGetValue(nickname, out var occupant))
            {
                return Task.FromResult(false);
            }

            return this.protocolHandler.ChangeRoomOccupantAffiliationAsync(this.Jid, occupant.FullJid.ToBareJid(), Affiliation.Admin, reason);
        }

        public Task<bool> GrantAdminStatusAsync(string nickname, string reason = null)
        {
            if (!this.occupants.TryGetValue(nickname, out var occupant))
            {
                return Task.FromResult(false);
            }

            return this.protocolHandler.ChangeRoomOccupantAffiliationAsync(this.Jid, occupant.FullJid.ToBareJid(), Affiliation.Admin, reason);
        }

        public Task<bool> RevokeAdminStatusAsync(string nickname, string reason = null)
        {
            if (!this.occupants.TryGetValue(nickname, out var occupant))
            {
                return Task.FromResult(false);
            }

            //UNDONE
            //"An owner might want to revoke a user's admin status; this is done by changing the user's affiliation to something other
            // than "admin" or "owner" (typically to "member" in a members-only room or to "none" in other types of room)."

            return this.protocolHandler.ChangeRoomOccupantAffiliationAsync(this.Jid, occupant.FullJid.ToBareJid(), Affiliation.None, reason);
        }

        public async Task SendMessageToAllOccupantsAsync(string text)
        {
            if (await this.HandleIRCCommandAsync(text).ConfigureAwait(false))
                return;

            await this.protocolHandler.SendMessageToAllOccupantsAsync(this.Jid, text).ConfigureAwait(false);
        }

        internal void OnSubjectChange(string subject, string byNickname)
        {
            this.Subject = subject;
            this.SubjectChanged?.Invoke(this, (subject, byNickname));
        }

        internal void OnMessage(string messageText, string nickname, DateTime time = default)
        {
            this.NewMessage?.Invoke(this, (messageText, nickname, time));
        }

        private async Task<bool> HandleIRCCommandAsync(string text)
        {
            if (text.StartsWith("/"))
            {
                var splittedCmd = text.Split(' ', 2);
                if (splittedCmd.Length == 2)
                {
                    switch (splittedCmd[0])
                    {
                        case "/topic":
                            await this.ChangeSubjectAsync(splittedCmd[1]);
                            return true;
                        case "/ban":
                            await this.BanOccupantAsync(splittedCmd[1]);
                            return true;
                    }
                }
                else
                {
                    this.errorOccurred?.Invoke(this, "Incorrect command syntax");
                }

                return true;
            }

            return false;
        }
    }
}