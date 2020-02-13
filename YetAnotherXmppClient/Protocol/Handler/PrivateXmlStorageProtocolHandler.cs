using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Queries;

// XEP-0049: Private XML Storage

namespace YetAnotherXmppClient.Protocol.Handler
{
    internal class PrivateXmlStorageProtocolHandler : ProtocolHandlerBase, IAsyncQueryHandler<StorePrivateXmlQuery, Iq>, IAsyncQueryHandler<RetrievePrivateXmlQuery, string>
    {
        public PrivateXmlStorageProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters, IMediator mediator)
            : base(xmppStream, runtimeParameters, mediator)
        {
            this.Mediator.RegisterHandler<StorePrivateXmlQuery, Iq>(this);
            this.Mediator.RegisterHandler<RetrievePrivateXmlQuery, string>(this);
        }

        public async Task<bool> StoreAsync(XElement xElem)
        {
            var response = await this.InternalStoreAsync(xElem).ConfigureAwait(false);

            return response.Type == IqType.result;
        }

        private Task<Iq> InternalStoreAsync(XElement xElem)
        {
            var iq = new Iq(IqType.set, new XElement(XNames.private_query, xElem))
                         {
                             Id = Guid.NewGuid().ToString()
                         };
            return this.XmppStream.WriteIqAndReadReponseAsync(iq);
        }

        public async Task<XElement> RetrieveAsync(XName xName)
        {
            var iq = new Iq(IqType.get, new XElement(XNames.private_query, new XElement(xName)))
                         {
                             Id = Guid.NewGuid().ToString()
                         };
            var response = await this.XmppStream.WriteIqAndReadReponseAsync(iq).ConfigureAwait(false);

            return response.Element(XNames.private_query).FirstElement();
        }

        Task<Iq> IAsyncQueryHandler<StorePrivateXmlQuery, Iq>.HandleQueryAsync(StorePrivateXmlQuery query)
        {
            return this.InternalStoreAsync(XElement.Parse(query.Xml));
        }

        async Task<string> IAsyncQueryHandler<RetrievePrivateXmlQuery, string>.HandleQueryAsync(RetrievePrivateXmlQuery query)
        {
            var xElem = await this.RetrieveAsync(query.XName).ConfigureAwait(false);
            return xElem.ToString();
        }
    }
}
