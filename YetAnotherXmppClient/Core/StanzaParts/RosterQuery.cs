using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;

namespace YetAnotherXmppClient.Core.StanzaParts
{
    public class RosterQuery : XElement
    {
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