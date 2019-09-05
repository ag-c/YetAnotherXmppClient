using System.Xml.Linq;

namespace YetAnotherXmppClient.Core.StanzaParts
{
    class DiscoInfoFeature : XElement
    {
        public DiscoInfoFeature(string name)
            : base(XNames.discoinfo_feature, new XAttribute("var", name))
        {

        }
    }
}
