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
using YetAnotherXmppClient.Protocol.Handler.ServiceDiscovery;
using static YetAnotherXmppClient.Expectation;

//XEP-0030

namespace YetAnotherXmppClient.Protocol.Handler
{
    namespace ServiceDiscovery
    {
        public class Feature
        {
            public string Var { get; set; }
        }

        public class Identity
        {
            public string Category { get; set; }
            public string Type { get; set; }
            public string Name { get; set; }
        }

        //UNDONE move to other namespace
        public class EntityInfo
        {
            public string Jid { get; set; }

            public IEnumerable<Identity> Identities { get; set; }
            public IEnumerable<Feature> Features { get; set; }

            public IEnumerable<EntityInfo> Children { get; set; }
        }

        public class Item
        {
            public string Jid { get; set; }
            public string Name { get; set; }
            public string Node { get; set; }
        }
    }

    
    internal sealed class ServiceDiscoveryProtocolHandler : ProtocolHandlerBase, IIqReceivedCallback, 
                                                   IAsyncQueryHandler<EntityInformationTreeQuery, EntityInfo>,
                                                   IAsyncQueryHandler<EntityInformationQuery, EntityInfo>,
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

        public async Task<ServiceDiscovery.EntityInfo> QueryEntityInformationTreeAsync(string jid)
        {
            //if given jid equals null, use the currently logged in users server
            if (jid == null)
            {
                var _jid = new Jid(this.RuntimeParameters["jid"]);
                jid = _jid.Server;
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

        public async Task<ServiceDiscovery.EntityInfo> QueryEntityInformationAsync(string jid, string node = null)
        { 
            var iq = new Iq(IqType.get, new XElement(XNames.discoinfo_query, node == null ? null : new XAttribute("node", node)))
            {
                From = this.RuntimeParameters["jid"],
                To = jid
            };

            var iqResp = await this.XmppStream.WriteIqAndReadReponseAsync(iq).ConfigureAwait(false);

            var queryElem = iqResp.Element(XNames.discoinfo_query);
            var entityInfo = new ServiceDiscovery.EntityInfo
            {
                Jid = iqResp.From,
                Identities = queryElem.Elements(XNames.discoinfo_identity).Select(xe => new ServiceDiscovery.Identity
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

        public async Task<IEnumerable<ServiceDiscovery.Item>> DiscoverItemsAsync(string entityId)
        {
            var iq = new Iq(IqType.get, new XElement(XNames.discoitems_query))
            {
                From = this.RuntimeParameters["jid"],
                To = entityId //this.RuntimeParameters["jid"].ToBareJid()
            };

            var iqResp = await this.XmppStream.WriteIqAndReadReponseAsync(iq).ConfigureAwait(false);

            Expect(IqType.result, iqResp.Type, iqResp);

            return iqResp.Element(XNames.discoitems_query).Elements(XNames.discoitems_item).Select(xe => new ServiceDiscovery.Item
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
                from: this.RuntimeParameters["jid"]);

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
            return this.QueryEntityInformationAsync(query.FullJid);
        }

        async Task<bool> IAsyncQueryHandler<EntitySupportsFeatureQuery, bool>.HandleQueryAsync(EntitySupportsFeatureQuery query)
        {
            if (string.IsNullOrWhiteSpace(query.FullJid) || string.IsNullOrWhiteSpace(query.ProtocolNamespace))
                return false;

            if (this.entityInformations.TryGetValue(query.FullJid, out var entityInfo))
            {
                return entityInfo.Features.Any(f => f.Var == query.ProtocolNamespace);
            }
            else if (this.entityInformationTrees.TryGetValue(query.FullJid, out var entityInfoTree))
            {
                //UNDONE checking features of Children also?
                return entityInfoTree.Features.Any(f => f.Var == query.ProtocolNamespace);
            }

            //UNDONE is it enough without checking the Children too?
            var info = await this.QueryEntityInformationAsync(query.FullJid).ConfigureAwait(false);
            return info.Features.Any(f => f.Var == query.ProtocolNamespace);
        }
    }
}
