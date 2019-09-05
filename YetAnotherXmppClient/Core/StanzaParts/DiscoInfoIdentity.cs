using System.Xml.Linq;

namespace YetAnotherXmppClient.Core.StanzaParts
{
    class DiscoInfoIdentity : XElement
    {
        public DiscoInfoIdentity(string category, string type, string name)
            : base(XNames.discoinfo_identity, new XAttribute("category", category), new XAttribute("type", type), new XAttribute("name", name))
        {

        }
    }
}
