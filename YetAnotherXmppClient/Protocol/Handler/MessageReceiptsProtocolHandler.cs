using System.Collections.Generic;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Extensions;
using static YetAnotherXmppClient.Expectation;

namespace YetAnotherXmppClient.Protocol.Handler
{
    //XEP-0184: Message Receipts
    public class MessageReceiptsProtocolHandler : ProtocolHandlerBase, IMessageReceivedCallback
    {
        public MessageReceiptsProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters)
            : base(xmppStream, runtimeParameters)
        {
            this.XmppStream.RegisterMessageContentCallback(XNames.receipts_request, this);
        }

        //UNDONE async?
        //XEP-0184/3. Protocol Format
        async void IMessageReceivedCallback.MessageReceived(Message message)
        {
            // does <message/> contain <request/> and id-attribute?
            if (message.HasElement(XNames.receipts_request)
                && message.HasAttribute("id"))
            {
                Expect(this.RuntimeParameters["jid"], message.To, message);

                var messageResp = new Message(new XElement(XNames.receipts_received))
                {
                    Id = message.Id,
                    From = this.RuntimeParameters["jid"], //alt. copy from to-attribute
                    To = message.From
                };
                await this.XmppStream.WriteElementAsync(messageResp);
            }
        }
    }
}
