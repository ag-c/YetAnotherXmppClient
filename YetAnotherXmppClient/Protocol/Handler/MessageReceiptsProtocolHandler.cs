using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Infrastructure;

namespace YetAnotherXmppClient.Protocol.Handler
{
    //XEP-0184: Message Receipts
    public class MessageReceiptsProtocolHandler : ProtocolHandlerBase, IMessageReceivedCallback
    {
        public MessageReceiptsProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters, IMediator mediator)
            : base(xmppStream, runtimeParameters, mediator)
        {
            this.XmppStream.RegisterMessageContentCallback(XNames.receipts_request, this);
        }

        //XEP-0184/3. Protocol Format
        async Task IMessageReceivedCallback.HandleMessageReceivedAsync(Message message)
        {
            // does <message/> contain <request/> and id-attribute?
            if (message.HasElement(XNames.receipts_request)
                && message.HasAttribute("id"))
            {
                var response = message.CreateResponse(
                    content: new XElement(XNames.receipts_received), 
                    from: this.RuntimeParameters["jid"]);

                await this.XmppStream.WriteElementAsync(response).ConfigureAwait(false);
            }
        }
    }
}
