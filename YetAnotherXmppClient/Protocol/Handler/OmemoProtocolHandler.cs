using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;

namespace YetAnotherXmppClient.Protocol.Handler
{
    class OmemoProtocolHandler : ProtocolHandlerBase, IMessageReceivedCallback
    {
        private readonly PepProtocolHandler pepHandler;

        public OmemoProtocolHandler(PepProtocolHandler pepHandler, AsyncXmppStream xmppStream, Dictionary<string, string> runtimeParameters) 
            : base(xmppStream, runtimeParameters)
        {
            this.pepHandler = pepHandler;

            this.XmppStream.RegisterMessageContentCallback(XNames.pubsub_event, this);
        }

        public Task InitializeAsync()
        {
            return this.pepHandler.SubscribeToNodeAsync("eu.siacs.conversations.axolotl.devicelist");
        }

        public void MessageReceived(XElement messageElem)
        {
            
        }
    }
}
