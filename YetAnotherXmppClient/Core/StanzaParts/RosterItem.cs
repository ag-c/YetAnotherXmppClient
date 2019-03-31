using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

namespace YetAnotherXmppClient.Core.StanzaParts
{
    public class RosterItem : XElement
    {
        public RosterItem(string bareJid, bool remove)
            : base(XNames.roster_item,
                new XAttribute("jid", bareJid),
                new XAttribute("subscription", "remove"))
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
    }
}
