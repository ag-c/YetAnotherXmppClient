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
using YetAnotherXmppClient.Infrastructure.Queries;
using static YetAnotherXmppClient.Expectation;

//XEP-0030

namespace YetAnotherXmppClient.Protocol.Handler.ServiceDiscovery
{
    public sealed class ServiceDiscoveryProtocolHandler : ProtocolHandlerBase, IIqReceivedCallback, 
                                                            IAsyncQueryHandler<EntityInformationTreeQuery, EntityInfo>,
                                                            IAsyncQueryHandler<EntityInformationQuery, EntityInfo>,
                                                            IAsyncQueryHandler<EntityItemsQuery, IEnumerable<Item>>,
                                                            IAsyncQueryHandler<EntitySupportsFeatureQuery, bool>,
                                                            ICommandHandler<RegisterFeatureCommand>
    {
        private readonly List<string> registeredFeatureProtocolNamespaces = new List<string>();

        //<jid, entity info WITHOUT expanded children items>
        private readonly Dictionary<string, EntityInfo> entityInformations = new Dictionary<string, EntityInfo>();

        //<jid, entity info WITH expanded children items>
        private readonly Dictionary<string, EntityInfo> entityInformationTrees = new Dictionary<string, EntityInfo>();

        public ServiceDiscoveryProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters, IMediator mediator) 
            : base(xmppStream, runtimeParameters, mediator)
        {
            this.XmppStream.RegisterIqNamespaceCallback(XNamespaces.discoinfo, this);
            this.Mediator.RegisterHandler<EntityInformationTreeQuery, EntityInfo>(this);
            this.Mediator.RegisterHandler<RegisterFeatureCommand>(this);
        }

        public async Task<EntityInfo> QueryEntityInformationTreeAsync(string jid = null)
        {
            //if given jid equals null, use the currently logged in users server
            if (jid == null)
            {
                jid = new Jid(this.RuntimeParameters["jid"]).Server;
            }

            if (this.entityInformationTrees.TryGetValue(jid, out var existingEntityInfoTree))
            {
                return existingEntityInfoTree;
            }

            var rootInfo = await this.QueryEntityInformationAsync(jid).ConfigureAwait(false);

            var items = await this.DiscoverItemsAsync(jid).ConfigureAwait(false);
            //UNDONE recursive
            rootInfo.Children = await Task.WhenAll(items.Select(item => this.QueryEntityInformationAsync(item.Jid))).ConfigureAwait(false);

            this.entityInformationTrees[jid] = rootInfo;

            return rootInfo;
        }

        public async Task<EntityInfo> QueryEntityInformationAsync(string jid, string node = null)
        { 
            var iq = new Iq(IqType.get, new XElement(XNames.discoinfo_query, node == null ? null : new XAttribute("node", node)))
            {
                From = this.RuntimeParameters["jid"],
                To = jid
            };

            var iqResp = await this.XmppStream.WriteIqAndReadReponseAsync(iq).ConfigureAwait(false);

            var queryElem = iqResp.Element(XNames.discoinfo_query);
            var entityInfo = new EntityInfo
            {
                Jid = iqResp.From,
                Identities = queryElem.Elements(XNames.discoinfo_identity).Select(xe => new Identity
                {
                    Category = xe.Attribute("category").Value,
                    Type = xe.Attribute("type").Value,
                    Name = xe.Attribute("name")?.Value,
                }),
                Features = queryElem.Elements(XNames.discoinfo_feature).Select(xe => new ServiceDiscovery.Feature
                {
                    Var = xe.Attribute("var").Value,
                })
            };

            this.entityInformations[jid] = entityInfo;

            return entityInfo;
        }

        public async Task<IEnumerable<Item>> DiscoverItemsAsync(string entityId, string node = null)
        {
            var iq = new Iq(IqType.get, new XElement(XNames.discoitems_query, node == null ? null : new XAttribute("node", node)))
            {
                From = this.RuntimeParameters["jid"],
                To = entityId //this.RuntimeParameters["jid"].ToBareJid()
            };

            var iqResp = await this.XmppStream.WriteIqAndReadReponseAsync(iq).ConfigureAwait(false);

            Expect(IqType.result, iqResp.Type, iqResp);

            return iqResp.Element(XNames.discoitems_query).Elements(XNames.discoitems_item).Select(xe => new Item
            {
                Jid = xe.Attribute("jid").Value,
                Name = xe.Attribute("name")?.Value,
                Node = xe.Attribute("node")?.Value,
            });
        }

        async Task IIqReceivedCallback.HandleIqReceivedAsync(Iq iq)
        {
            Expect(() => iq.HasElement(XNames.discoinfo_query), iq);
            
            var response = iq.CreateResultResponse(
                content: new XElement(XNames.discoinfo_query,
                            new DiscoInfoIdentity("client", "pc", "YetAnotherXmppClient"),
                            new DiscoInfoFeature("http://jabber.org/protocol/disco#info"),
                            //new DiscoInfoFeature("eu.siacs.conversations.axolotl.devicelist+notify"),
                            this.registeredFeatureProtocolNamespaces.Select(name => new DiscoInfoFeature(name))),
                @from: this.RuntimeParameters["jid"]);

            await this.XmppStream.WriteElementAsync(response).ConfigureAwait(false);

        }

        Task<EntityInfo> IAsyncQueryHandler<EntityInformationTreeQuery, EntityInfo>.HandleQueryAsync(EntityInformationTreeQuery query)
        {
            return this.QueryEntityInformationTreeAsync(query.Jid);
        }

        public async Task<bool> IsFeatureSupportedAsync(string name)
        {
            var jid = new Jid(this.RuntimeParameters["jid"]);
            var rootInfo = await this.QueryEntityInformationAsync(jid.Server).ConfigureAwait(false);
            return rootInfo.Features.Any(f => f.Var == name);
        }

        void ICommandHandler<RegisterFeatureCommand>.HandleCommand(RegisterFeatureCommand command)
        {
            this.registeredFeatureProtocolNamespaces.Add(command.ProtocolNamespace);
        }

        Task<EntityInfo> IAsyncQueryHandler<EntityInformationQuery, EntityInfo>.HandleQueryAsync(EntityInformationQuery query)
        {
            return this.QueryEntityInformationAsync(query.Jid);
        }

        async Task<bool> IAsyncQueryHandler<EntitySupportsFeatureQuery, bool>.HandleQueryAsync(EntitySupportsFeatureQuery query)
        {
            if (string.IsNullOrWhiteSpace(query.ProtocolNamespace))
                return false;

            var fullJid = query.Jid ?? new Jid(this.RuntimeParameters["jid"]).Server;

            if (this.entityInformations.TryGetValue(fullJid, out var entityInfo))
            {
                return entityInfo.Features.Any(f => f.Var == query.ProtocolNamespace);
            }
            
            if (this.entityInformationTrees.TryGetValue(fullJid, out var entityInfoTree))
            {
                //UNDONE checking features of Children also?
                return entityInfoTree.Features.Any(f => f.Var == query.ProtocolNamespace);
            }

            //UNDONE is it enough without checking the Children too?
            var info = await this.QueryEntityInformationAsync(fullJid).ConfigureAwait(false);
            return info.Features.Any(f => f.Var == query.ProtocolNamespace);
        }

        Task<IEnumerable<Item>> IAsyncQueryHandler<EntityItemsQuery, IEnumerable<Item>>.HandleQueryAsync(EntityItemsQuery query)
        {
            return this.DiscoverItemsAsync(query.Jid);
        }
    }
}
