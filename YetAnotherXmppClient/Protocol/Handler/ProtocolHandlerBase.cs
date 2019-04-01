using System.Collections.Generic;
using YetAnotherXmppClient.Core;

namespace YetAnotherXmppClient.Protocol.Handler
{
    public abstract class ProtocolHandlerBase
    {
        protected AsyncXmppStream XmppStream { get; }
        protected Dictionary<string, string> RuntimeParameters { get; }

        protected ProtocolHandlerBase(AsyncXmppStream xmppStream, Dictionary<string, string> runtimeParameters)
        {
            this.XmppStream = xmppStream;
            this.RuntimeParameters = runtimeParameters;
        }
    }
}
