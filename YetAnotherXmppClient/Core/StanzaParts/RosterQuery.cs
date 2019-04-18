using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using YetAnotherXmppClient.Extensions;

namespace YetAnotherXmppClient.Core.StanzaParts
{
    public class RosterQuery : XElement
    {
        private IEnumerable<RosterItem> items;
        public IEnumerable<RosterItem> Items => this.items ?? (this.items = this.Elements(XNames.roster_item)?.Select(RosterItem.FromXElement));

        public string Ver => this.Attribute("ver")?.Value;

        //copy constructor
        private RosterQuery(XElement rosterItemXElem)
            : base(XNames.roster_query, rosterItemXElem.ElementsAndAttributes())
        {
        }

        public RosterQuery() 
            : base(XNames.roster_query)
        {
        }

        public RosterQuery(string bareJid, bool remove)
            : base(XNames.roster_query, new RosterItem(bareJid, remove))
        {
            Debug.Assert(remove);
        }

        public RosterQuery(string bareJid, string name, IEnumerable<string> groupNames)
            : base(XNames.roster_query, new RosterItem(bareJid, name, groupNames))
        {
        }
    }
}