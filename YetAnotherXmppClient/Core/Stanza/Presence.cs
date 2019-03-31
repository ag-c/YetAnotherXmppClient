using System;
using System.Xml.Linq;
using YetAnotherXmppClient.Core.StanzaParts;

namespace YetAnotherXmppClient.Core.Stanza
{
    public enum PresenceType
    {
        //The sender wishes to subscribe to the recipient's presence.
        subscribe,

        //The sender has allowed the recipient to receive their presence.
        subscribed,

        //The sender is unsubscribing from the receiver's presence.
        unsubscribe,

        //The subscription request has been denied or a previously granted subscription has been canceled.
        unsubscribed,

        unavailable
    }

    public class Presence : XElement
    {
        public string Id
        {
            get => this.Attribute("id")?.Value;
            set => this.SetAttributeValue("id", value);
        }

        public PresenceType Type
        {
            get => (PresenceType)Enum.Parse(typeof(PresenceType), this.Attribute("type").Value);
            set => this.SetAttributeValue("type", value.ToString());
        }

        public string To
        {
            get => this.Attribute("to")?.Value;
            set => this.SetAttributeValue("to", value);
        }

        public Presence() : base("presence")
        {
            this.Id = Guid.NewGuid().ToString();
        }

        public Presence(PresenceShow? show=null, string status = null) 
            : base("presence", 
                show.HasValue ? new XElement("show", show.ToString()) : null, 
                status!=null ? new XElement("status", status) : null)
        {
            this.Id = Guid.NewGuid().ToString();
        }

        public Presence(PresenceType type) : base("presence")
        {
            this.Id = Guid.NewGuid().ToString();
            this.Type = type;
        }

        public static implicit operator string(Presence presence)
        {
            return presence.ToString();
        }
    }
}
