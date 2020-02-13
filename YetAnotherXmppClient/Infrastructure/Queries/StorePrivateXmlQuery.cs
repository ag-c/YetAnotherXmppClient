using YetAnotherXmppClient.Core.Stanza;

namespace YetAnotherXmppClient.Infrastructure.Queries
{
    public class StorePrivateXmlQuery : IQuery<Iq>
    {
        public string Xml { get; }

        public StorePrivateXmlQuery(string xml)
        {
            this.Xml = xml;
        }
    }
}
