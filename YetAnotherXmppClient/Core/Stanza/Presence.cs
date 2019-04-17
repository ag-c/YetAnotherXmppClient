using System;
using System.Xml.Linq;
using YetAnotherXmppClient.Core.StanzaParts;
using YetAnotherXmppClient.Extensions;

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

        public PresenceType? Type
        {
            get => this.HasAttribute("type") ? (PresenceType)Enum.Parse(typeof(PresenceType), this.Attribute("type").Value) : (PresenceType?)null;
            set => this.SetAttributeValue("type", value.ToString());
        }

        public string From
        {
            get => this.Attribute("from")?.Value;
            set => this.SetAttributeValue("from", value);
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

        private Presence(params object[] content) : base("{jabber:client}presence", content) //{jabber:client}
        {
        }

        public static Presence FromXElement(XElement xElem)
        {
            var presence = new Presence(xElem.Elements());
            foreach (var attr in xElem.Attributes())
                presence.SetAttributeValue(attr.Name, attr.Value);
            return presence;
        }

        public static implicit operator string(Presence presence)
        {
            return presence.ToString();
        }
    }
}
