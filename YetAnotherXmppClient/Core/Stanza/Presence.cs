using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using YetAnotherXmppClient.Core.StanzaParts;
using YetAnotherXmppClient.Extensions;

namespace YetAnotherXmppClient.Core.Stanza
{
    public enum PresenceType
    {
        //A request for an entity's current presence; SHOULD be generated only by a server on behalf of a user.
        probe,

        //The sender wishes to subscribe to the recipient's presence.
        subscribe,

        //The sender has allowed the recipient to receive their presence.
        subscribed,

        //The sender is unsubscribing from the receiver's presence.
        unsubscribe,

        //The subscription request has been denied or a previously granted subscription has been canceled.
        unsubscribed,

        //The sender is no longer available for communication.
        unavailable,

        error
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
            get => EnumHelper.Parse<PresenceType>(this.Attribute("type")?.Value);
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

        public PresenceShow? Show => EnumHelper.Parse<PresenceShow>(this.Attribute("show")?.Value);

        public IEnumerable<string> Stati => this.Elements("status")?.Select(xe => xe.Value);

        
        public int? Priority
        {
            get
            {
                if (int.TryParse(this.Element("priority")?.Value, out var prio))
                {
                    return prio;
                }
                return null;
            }
        }

        //copy ctor
        private Presence(XElement presenceXElem)
            : base("{jabber:client}presence", presenceXElem.ElementsAndAttributes())
        {
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

        public static Presence FromXElement(XElement xElem)
        {
            Expectation.Expect("presence", xElem.Name.LocalName, xElem);
            return new Presence(xElem);
        }
    }
}
