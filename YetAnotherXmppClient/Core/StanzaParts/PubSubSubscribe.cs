using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace YetAnotherXmppClient.Core.StanzaParts
{
    public class PubSubSubscribe : XElement
    {
        public PubSubSubscribe(string nodeId, string jid) : base(XNames.pubsub_pubsub, 
            new XElement(XNames.pubsub_subscribe, new XAttribute("node", nodeId), new XAttribute("jid", jid)))
        {
            
        }
    }
}
