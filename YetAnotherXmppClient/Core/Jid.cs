using System;

namespace YetAnotherXmppClient.Core
{
    public class Jid
    {
        public string Local { get; }
        public string Server { get; }
        public string Resource { get; }

        public Jid(string jid)
        {
            var parts = jid.Split('@', '/');
            
            if(parts.Length != 3)
                throw new ArgumentException("JID is not in the form of <localpart>@<serverpart>/<resource>");

            this.Local = parts[0];
            this.Server = parts[1];
            this.Resource = parts[2];
        }

        public static implicit operator string(Jid jid)
        {
            return jid.ToString();
        }
        
        public override string ToString()
        {
            return $"{this.Local}@{this.Server}/{this.Resource}";
        }
    }
}