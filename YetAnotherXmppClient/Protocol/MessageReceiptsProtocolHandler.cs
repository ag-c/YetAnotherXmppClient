using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Extensions;
using static YetAnotherXmppClient.Expectation;

namespace YetAnotherXmppClient.Protocol
{
    //XEP-0184: Message Receipts
    public class MessageReceiptsProtocolHandler : IMessageReceivedCallback
    {
        private readonly AsyncXmppStream xmppStream;
        private readonly Dictionary<string, string> runtimeParameters;

        public MessageReceiptsProtocolHandler(AsyncXmppStream xmppStream, Dictionary<string, string> runtimeParameters)
        {
            this.xmppStream = xmppStream;
            this.runtimeParameters = runtimeParameters;

            this.xmppStream.RegisterMessageContentCallback(XNames.receipts_request, this);
        }

        //UNDONE async?
        //XEP-0184/3. Protocol Format
        async void IMessageReceivedCallback.MessageReceived(XElement messageElem)
        {
            // does <message/> contain <request/> and id-attribute?
            if (messageElem.HasElement(XNames.receipts_request)
                && messageElem.HasAttribute("id"))
            {
                Expect(this.runtimeParameters["jid"], messageElem.Attribute("to")?.Value, messageElem);

                var message = new Message(new XElement(XNames.receipts_received))
                {
                    Id = messageElem.Attribute("id").Value,
                    From = this.runtimeParameters["jid"], //alt. copy from to-attribute
                    To = messageElem.Attribute("from").Value
                };
                await this.xmppStream.WriteAsync(message);
            }
        }
    }
}
