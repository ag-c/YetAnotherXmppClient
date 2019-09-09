using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Infrastructure;

//XEP-0191: Blocking Command

namespace YetAnotherXmppClient.Protocol.Handler
{
    //UNDONE move to stanzaparts?
    public class Blocklist : XElement
    {
        //private IEnumerable<Core.StanzaParts.RosterItem> items;
        public IEnumerable<string> Jids => this.Elements(XNames.blocking_item)?.Select(xe => xe.Attribute("jid").Value);


        //copy constructor
        private Blocklist(XElement blocklistXElem)
            : base(XNames.blocking_blocklist, blocklistXElem.ElementsAndAttributes())
        {
        }

        //public RosterQuery()
        //    : base(XNames.roster_query)
        //{
        //}
    }

    public class BlockingProtocolHandler : ProtocolHandlerBase
    {
        public BlockingProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters, IMediator mediator/*, ServiceDiscoveryProtocolHandler serviceDiscoveryProtocolHandler*/)
            : base(xmppStream, runtimeParameters, mediator)
        {
        }

        public async Task<IEnumerable<string>> RetrieveBlockListAsync()
        {
            var iqResp = await this.XmppStream.WriteIqAndReadReponseAsync(new IqGet(new XElement(XNames.blocking_blocklist)));
            var blocklist = iqResp.GetContent<Blocklist>();
            return blocklist.Jids;
        }

        public async Task<bool> BlockAsync(string bareJid)
        {
            var iq = new IqSet(new XElement(XNames.blocking_block, new XElement(XNames.blocking_item, new XAttribute("jid", bareJid.ToBareJid()))));
            var iqResp = await this.XmppStream.WriteIqAndReadReponseAsync(iq);
            return iqResp.Type == IqType.result;
        }

        public async Task<bool> UnblockAsync(string bareJid)
        {
            var iq = new IqSet(new XElement(XNames.blocking_unblock, new XElement(XNames.blocking_item, new XAttribute("jid", bareJid.ToBareJid()))));
            var iqResp = await this.XmppStream.WriteIqAndReadReponseAsync(iq);
            return iqResp.Type == IqType.result;
        }

        public async Task<bool> UnblockAllAsync()
        {
            var iq = new IqSet(new XElement(XNames.blocking_unblock));
            var iqResp = await this.XmppStream.WriteIqAndReadReponseAsync(iq);
            return iqResp.Type == IqType.result;
        }
    }
}
