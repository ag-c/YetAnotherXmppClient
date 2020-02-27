using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Commands;
using YetAnotherXmppClient.Infrastructure.Queries;

//XEP-0012: Last Activity

namespace YetAnotherXmppClient.Protocol.Handler
{
    public class LastActivityInfo
    {
        public uint Seconds { get; set; }
        public string Status { get; set; }
    }

    internal sealed class LastActivityProtocolHandler : ProtocolHandlerBase, IIqReceivedCallback, 
                                               IAsyncQueryHandler<LastActivityQuery, LastActivityInfo>, 
                                               ICommandHandler<AttestActivityCommand>
    {
        private DateTime lastActivity = DateTime.Now;

        public LastActivityProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters, IMediator mediator)
            : base(xmppStream, runtimeParameters, mediator)
        {
            this.XmppStream.RegisterIqContentCallback(XNames.last_query, this);
            this.Mediator.RegisterHandler<LastActivityQuery, LastActivityInfo>(this);
            this.Mediator.RegisterHandler<AttestActivityCommand>(this);
            this.Mediator.RegisterFeature(ProtocolNamespaces.LastActivity);
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

        Task<LastActivityInfo> IAsyncQueryHandler<LastActivityQuery, LastActivityInfo>.HandleQueryAsync(LastActivityQuery query)
        {
            return this.QueryAsync(query.Jid);
        }

        Task IIqReceivedCallback.HandleIqReceivedAsync(Iq iq)
        {
            var secondsSinceLastActivity = (DateTime.Now - this.lastActivity).TotalSeconds;
            var iqResponse = new Iq(IqType.result, new XElement(XNames.last_query, new XAttribute("seconds", secondsSinceLastActivity)));
            return this.XmppStream.WriteElementAsync(iqResponse);
        }

        void ICommandHandler<AttestActivityCommand>.HandleCommand(AttestActivityCommand command)
        {
            this.lastActivity = DateTime.Now;
        }
    }
}
