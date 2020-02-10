using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Core.StanzaParts;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Commands;
using YetAnotherXmppClient.Infrastructure.Events;
using YetAnotherXmppClient.Infrastructure.Queries;
using YetAnotherXmppClient.Protocol.Handler.ServiceDiscovery;

// XEP-0163: Personal Eventing Protocol

namespace YetAnotherXmppClient.Protocol.Handler
{
    internal class PepProtocolHandler : ProtocolHandlerBase, IMessageReceivedCallback, IAsyncCommandHandler<PublishEventCommand>
    {
        public PepProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters, IMediator mediator)
            : base(xmppStream, runtimeParameters, mediator)
        {
            this.XmppStream.RegisterMessageContentCallback(XNames.pubsubevent_event, this);
            this.Mediator.RegisterHandler<PublishEventCommand>(this);
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

        //XEP-0163/6.1
        public async Task<bool> DetermineSupportAsync()
        {
            var ownBareJid = this.RuntimeParameters["jid"].ToBareJid();
            var entityInfo = await this.Mediator.QueryAsync<EntityInformationQuery, EntityInfo>(new EntityInformationQuery(ownBareJid)).ConfigureAwait(false);

            return entityInfo.Identities.Any(id => id.Category == "pubsub" && id.Type == "pep");
        }

        public async Task PublishEventAsync(string node, string itemId, XElement content)
        {
            //var nodeId = Guid.NewGuid().ToString();
            //var itemId = (string)null;
            var iq = new Iq(IqType.set, new PubSubPublish(node, itemId, content));
        }

        public async Task SubscribeToNodeAsync(string nodeId)
        {
            var iq = new Iq(IqType.set, new PubSubSubscribe(nodeId, this.RuntimeParameters["jid"].ToBareJid()))
            {
                From = this.RuntimeParameters["jid"],
                To = this.RuntimeParameters["jid"].ToBareJid() //UNDONE only server?
            };

            var iqResp = await this.XmppStream.WriteIqAndReadReponseAsync(iq).ConfigureAwait(false);
        }

        Task IAsyncCommandHandler<PublishEventCommand>.HandleCommandAsync(PublishEventCommand command)
        {
            return this.PublishEventAsync(command.Node, null, command.Content);
        }
    }
}
