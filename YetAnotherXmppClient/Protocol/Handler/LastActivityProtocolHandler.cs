using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Queries;

//XEP-0012: Last Activity

namespace YetAnotherXmppClient.Protocol.Handler
{
    public class LastActivityInfo
    {
        public uint Seconds { get; set; }
        public string Status { get; set; }
    }

    public class LastActivityProtocolHandler : ProtocolHandlerBase, IAsyncQueryHandler<LastActivityQuery, LastActivityInfo>
    {
        public LastActivityProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters, IMediator mediator)
            : base(xmppStream, runtimeParameters, mediator)
        {
            this.Mediator.RegisterHandler<LastActivityQuery, LastActivityInfo>(this);
        }

        public async Task<LastActivityInfo> QueryAsync(string jid)
        {
            var iq = new Iq(IqType.get, new XElement(XNames.last_query));

            var iqResp = await this.XmppStream.WriteIqAndReadReponseAsync(iq).ConfigureAwait(false);

            //UNDONE check for error element (see Examples 7 & 9)

            var queryElem = iqResp.Element(XNames.last_query);
            var status = queryElem.Value;
            var secondsStr = queryElem.Attribute("seconds").Value;

            return new LastActivityInfo
                       {
                           Seconds = uint.Parse(secondsStr),
                           Status = status
                       };
        }

        public Task<LastActivityInfo> HandleQueryAsync(LastActivityQuery query)
        {
            return this.QueryAsync(query.Jid);
        }
    }
}
