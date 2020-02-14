using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Events;

// XEP-0060: Publish-Subscribe

namespace YetAnotherXmppClient.Protocol.Handler
{
    public class PubSubItems : Collection<PubSubItem>
    {
        public string Node { get; }

        public PubSubItems(string node)
        {
            this.Node = node;
        }
    }

    public class PubSubItem : XElement
    {
        public string Id => this.Attribute("id").Value;

        private PubSubItem(XElement pubSubItemXElem)
            : base(pubSubItemXElem.Name, pubSubItemXElem.ElementsAndAttributes())
        {
        }

        public static PubSubItem FromXElement(XElement xElem)
        {
            Expectation.Expect(XNames.pubsub_item, xElem.Name, xElem);
            return new PubSubItem(xElem);
        }
    }

    internal class PublishSubscribeProtocolHandler : ProtocolHandlerBase, IMessageReceivedCallback
    {
        public PublishSubscribeProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters, IMediator mediator)
            : base(xmppStream, runtimeParameters, mediator)
        {
            this.XmppStream.RegisterMessageContentCallback(XNames.pubsubevent_event, this);
        }

        async Task IMessageReceivedCallback.HandleMessageReceivedAsync(Message message)
        {
            var eventXElem = message.Element(XNames.pubsubevent_event);
            var itemsXElem = eventXElem.Element(XNames.pubsubevent_items);
            var items = new PubSubItems(itemsXElem.Attribute("node").Value);

            foreach (var itemXElem in itemsXElem.Elements(XNames.pubsubevent_item))
            {
                items.Add(PubSubItem.FromXElement(itemXElem));
            }

            await this.Mediator.PublishAsync(new PublishedEvent
                                           {
                                               Items = items
                                           }).ConfigureAwait(false);
        }
    }
}
