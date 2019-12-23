using System.Xml.Linq;
using YetAnotherXmppClient.Extensions;

namespace YetAnotherXmppClient.Core.StanzaParts
{
    public class Unblock : XElement
    {
        //null => unblock all
        public Unblock(string bareJid = null)
            : base(XNames.blocking_unblock, bareJid == null ? null : new XElement(XNames.blocking_item, new XAttribute("jid", bareJid.ToBareJid())))
        {
        }
    }
}
