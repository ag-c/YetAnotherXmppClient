using System.Collections.Generic;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Extensions;

namespace YetAnotherXmppClient.Protocol.Handler
{
    //XEP-0184: Message Receipts
    public class MessageReceiptsProtocolHandler : ProtocolHandlerBase, IMessageReceivedCallback
    {
        public MessageReceiptsProtocolHandler(AsyncXmppStream xmppStream, Dictionary<string, string> runtimeParameters)
            : base(xmppStream, runtimeParameters)
        {
            this.XmppStream.RegisterMessageContentCallback(XNames.receipts_request, this);
        }

        //UNDONE async?
        //XEP-0184/3. Protocol Format
        async void IMessageReceivedCallback.MessageReceived(XElement messageElem)
        {
            // does <message/> contain <request/> and id-attribute?
            if (messageElem.HasElement(XNames.receipts_request)
                && messageElem.HasAttribute("id"))
            {
                Expectation.Expect(this.RuntimeParameters["jid"], messageElem.Attribute("to")?.Value, messageElem);

                var message = new Message(new XElement(XNames.receipts_received))
                {
                    Id = messageElem.Attribute("id").Value,
                    From = this.RuntimeParameters["jid"], //alt. copy from to-attribute
                    To = messageElem.Attribute("from").Value
                };
                await this.XmppStream.WriteAsync(message);
            }
        }
    }
}
