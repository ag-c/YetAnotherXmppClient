using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.Internal;

namespace YetAnotherXmppClient.Core
{
    public class Jid
    {
        public string Local { get; }
        public string Server { get; }
        public string Resource { get; }
        public string Bare => this.Local + "@" + this.Server;

        public bool IsFull => !(new[] { this.Local, this.Server, this.Resource }.Any(string.IsNullOrWhiteSpace));

        public Jid(string jid)
        {
            var parts = jid.Split('@');
            
            if(parts.Length != 2)
                throw new ArgumentException("JID must have the form <localpart>@<serverpart>[/<resource>]");

            this.Local = parts[0];

            parts = parts[1].Split('/');
            this.Server = parts[0];
            if (parts.Length == 2)
                this.Resource = parts[1];
            else if (parts.Length != 1)
                throw new ArgumentException("JID must have the form <localpart>@<serverpart>[/<resource>]");
        }

        public static implicit operator string(Jid jid)
        {
            return jid.ToString();
        }
        
        public override string ToString()
        {
            return this.Resource != null ? 
                $"{this.Local}@{this.Server}/{this.Resource}"
                : $"{this.Local}@{this.Server}";
        }
    }
}