using System.Collections.Generic;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Infrastructure;

namespace YetAnotherXmppClient.Protocol.Handler
{
    public abstract class ProtocolHandlerBase
    {
        protected XmppStream XmppStream { get; }
        protected Dictionary<string, string> RuntimeParameters { get; }
        protected IMediator Mediator { get; }

        protected ProtocolHandlerBase(XmppStream xmppStream, Dictionary<string, string> runtimeParameters, IMediator mediator)
        {
            this.XmppStream = xmppStream;
            this.RuntimeParameters = runtimeParameters;
            this.Mediator = mediator;
        }
    }
}
