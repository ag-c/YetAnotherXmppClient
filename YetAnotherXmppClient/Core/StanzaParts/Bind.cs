using System.Xml.Linq;

namespace YetAnotherXmppClient.Core.StanzaParts
{
    public class Bind : XElement
    {
        public Bind(string resource = null)
            : base(XNames.bind_bind, resource == null ? null : new XElement(XNames.bind_resource, resource))
        {
        }
    }
}
