using System;
using System.Collections.Generic;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Infrastructure;

namespace YetAnotherXmppClient.Protocol.Handler
{
    class EntityTimeProtocolHandler : ProtocolHandlerBase, IIqReceivedCallback
    {
        public EntityTimeProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters, IMediator mediator) 
            : base(xmppStream, runtimeParameters, mediator)
        {
            this.XmppStream.RegisterIqNamespaceCallback(XNamespaces.time, this);
        }

        void IIqReceivedCallback.IqReceived(Iq iq)
        {
            throw new NotImplementedException();
        }
    }
}
