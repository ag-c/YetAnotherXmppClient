using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Xml.Linq;

namespace YetAnotherXmppClient.Protocol.Handler.MultiUserChat
{
    public class Occupant
    {
        public string Nickname { get; }
        public string FullJid { get; }
        public Affiliation Affiliation { get; }
        public Role Role { get; }

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
        Changed
    }

    public class Room
    {
        // <nickname, Occupant>
        private readonly ConcurrentDictionary<string, Occupant> occupants = new ConcurrentDictionary<string, Occupant>();
        private string errorText;

        public string Jid { get; }
        public string Name { get; } //UNDONE 

        public RoomType Type { get; internal set; }

        public Occupant Self { get; private set; }
        public IEnumerable<Occupant> Occupants => this.occupants.Values;

        //UNDONE TypeUpdated event?

        public event EventHandler<Occupant> SelfUpdated;
        public event EventHandler<(Occupant Occupant, OccupantUpdateCause Cause)> OccupantsUpdated;

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

        public Room(string jid)
        {
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

        internal void SetSelf(string nickname, string fullJid, Affiliation affiliation, Role role)
        {
            this.Self = new Occupant(nickname, fullJid, affiliation, role);
            this.SelfUpdated?.Invoke(this, this.Self);
        }

        internal void OnError(string errorText)
        {
            this.errorText = errorText;
            this.errorOccurred?.Invoke(this, errorText);
        }
    }
}