using System;
using System.Collections.Generic;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;

namespace YetAnotherXmppClient.Protocol.Handler
{
    class EntityTimeProtocolHandler : ProtocolHandlerBase, IIqReceivedCallback
    {
        public EntityTimeProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters) 
            : base(xmppStream, runtimeParameters)
        {
            this.XmppStream.RegisterIqNamespaceCallback(XNamespaces.time, this);
        }

        public void IqReceived(Iq iq)
        {
            throw new NotImplementedException();
        }
    }
}
