using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Core.StanzaParts;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Infrastructure;
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

    
    public class ServiceDiscoveryProtocolHandler : ProtocolHandlerBase, IIqReceivedCallback
    {
        public ServiceDiscoveryProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters, IMediator mediator) 
            : base(xmppStream, runtimeParameters, mediator)
        {
            this.XmppStream.RegisterIqNamespaceCallback(XNamespaces.discoinfo, this);
        }

        public async Task<ServiceDiscovery.EntityInfo> QueryEntityInformationTreeAsync()
        {
            var server = this.RuntimeParameters["jid"].ToBareJid().Split('@')[1];//UNDONE unsafe
            var rootInfo = await this.QueryEntityInformationAsync(server);

            var items = await this.DiscoverItemsAsync(server);
            //UNDONE recursive
            rootInfo.Children = await Task.WhenAll(items.Select(item => this.QueryEntityInformationAsync(item.Jid)));

            return rootInfo;
        }

        private async Task<ServiceDiscovery.EntityInfo> QueryEntityInformationAsync(string jid)
        { 
            var iq = new Iq(IqType.get, new XElement(XNames.discoinfo_query))
            {
                From = this.RuntimeParameters["jid"],
                To = jid
            };

            var iqResp = await this.XmppStream.WriteIqAndReadReponseAsync(iq);

            var queryElem = iqResp.Element(XNames.discoinfo_query);
            return new ServiceDiscovery.EntityInfo
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
        }

        public async Task<IEnumerable<ServiceDiscovery.Item>> DiscoverItemsAsync(string entityId)
        {
            var iq = new Iq(IqType.get, new XElement(XNames.discoitems_query))
            {
                From = this.RuntimeParameters["jid"],
                To = entityId //this.RuntimeParameters["jid"].ToBareJid()
            };

            var iqResp = await this.XmppStream.WriteIqAndReadReponseAsync(iq);

            Expect(IqType.result, iqResp.Type, iqResp);

            return iqResp.Element(XNames.discoitems_query).Elements(XNames.discoitems_item).Select(xe => new ServiceDiscovery.Item
            {
                Jid = xe.Attribute("jid").Value,
                Name = xe.Attribute("name")?.Value,
                Node = xe.Attribute("node")?.Value,
            });
        }

        async void IIqReceivedCallback.IqReceived(Iq iq)
        {
            Expect(() => iq.HasElement(XNames.discoinfo_query), iq);
            
            var response = iq.CreateResultResponse(
                content: new XElement(XNames.discoinfo_query,
                            new DiscoInfoIdentity("client", "pc", "YetAnotherXmppClient"),
                            new DiscoInfoFeature("http://jabber.org/protocol/disco#info"),
                            new DiscoInfoFeature("urn:xmpp:time"),
                            new DiscoInfoFeature("eu.siacs.conversations.axolotl.devicelist+notify"),
                            new DiscoInfoFeature("jabber:iq:version")),
                from: this.RuntimeParameters["jid"]);

            await this.XmppStream.WriteElementAsync(response);

        }
    }
}
