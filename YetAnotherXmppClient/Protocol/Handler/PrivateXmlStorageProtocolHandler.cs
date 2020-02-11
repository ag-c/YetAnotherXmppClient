using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Infrastructure;

// XEP-0049: Private XML Storage

namespace YetAnotherXmppClient.Protocol.Handler
{
    internal class PrivateXmlStorageProtocolHandler : ProtocolHandlerBase
    {
        public PrivateXmlStorageProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters, IMediator mediator)
            : base(xmppStream, runtimeParameters, mediator)
        {
        }

        public async Task<bool> StoreAsync(XElement xElem)
        {
            var iq = new Iq(IqType.set, new XElement(XNames.private_query, xElem))
                         {
                             Id = Guid.NewGuid().ToString()
                         };
            var response = await this.XmppStream.WriteIqAndReadReponseAsync(iq).ConfigureAwait(false);

            return response.Type == IqType.result;
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
    }
}
