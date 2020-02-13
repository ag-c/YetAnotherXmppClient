using System.Xml.Linq;

namespace YetAnotherXmppClient.Infrastructure.Queries
{
    public class RetrievePrivateXmlQuery : IQuery<string>
    {
        public XName XName { get; }

        public RetrievePrivateXmlQuery(XName xName)
        {
            this.XName = xName;
        }
    }
}
