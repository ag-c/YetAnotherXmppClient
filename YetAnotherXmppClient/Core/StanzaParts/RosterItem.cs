using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using YetAnotherXmppClient.Extensions;

namespace YetAnotherXmppClient.Core.StanzaParts
{
    public class RosterItem : XElement
    {
        public string Jid => this.Attribute("jid")?.Value;
        public string ItemName => this.Attribute("name")?.Value;
        public IEnumerable<string> Groups => this.Elements(XNames.roster_group)?.Select(xe => xe.Value);
        public SubscriptionState Subscription => this.HasAttribute("subscription") ? (SubscriptionState)Enum.Parse(typeof(SubscriptionState), this.Attribute("subscription").Value) : SubscriptionState.none;

        //copy constructor for element from server
        private RosterItem(XElement rosterItemXElem)
            : base(XNames.roster_item, rosterItemXElem.ElementsAndAttributes())
        {
        }

        public RosterItem(string bareJid, bool remove)
            : base(XNames.roster_item,
                new XAttribute("jid", bareJid),
                new XAttribute("subscription", "remove")) //UNDONE
        {
            Debug.Assert(remove);
        }

        public RosterItem(string bareJid, string name, IEnumerable<string> groupNames)
            : base(XNames.roster_item,
                new XAttribute("jid", bareJid),
                new XAttribute("name", name),
                groupNames?.Select(g => new XElement(XNames.roster_group, g)))
        {
        }

        public static RosterItem FromXElement(XElement xElem)
        {
            return new RosterItem(xElem);
        }
    }
}
