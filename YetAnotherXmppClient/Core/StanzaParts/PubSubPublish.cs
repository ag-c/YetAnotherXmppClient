using System.Xml.Linq;

namespace YetAnotherXmppClient.Core.StanzaParts
{
    public class PubSubPublish : XElement
    {
        public PubSubPublish(string nodeId, string itemId, object itemContent) 
            : base(XNames.pubsub_pubsub, new XElement(XNames.pubsub_publish, 
                new XAttribute("node", nodeId), new XElement(XNames.pubsub_item, itemId != null ? new XAttribute("id", itemId) : null, itemContent)))
        {
            
        }
    }
}
