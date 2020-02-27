using System.Collections.Generic;
using System.Diagnostics;
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

    internal sealed class BlockingProtocolHandler : ProtocolHandlerBase, 
        IAsyncQueryHandler<RetrieveBlockListQuery, IEnumerable<string>>,
        IAsyncQueryHandler<BlockQuery, bool>,
        IAsyncQueryHandler<UnblockQuery, bool>,
        IAsyncQueryHandler<UnblockAllQuery, bool>
    {
        public BlockingProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters, IMediator mediator)
            : base(xmppStream, runtimeParameters, mediator)
        {
            this.Mediator.RegisterHandler<RetrieveBlockListQuery, IEnumerable<string>>(this);
            this.Mediator.RegisterHandler<BlockQuery, bool>(this);
            this.Mediator.RegisterHandler<UnblockQuery, bool>(this);
            this.Mediator.RegisterHandler<UnblockAllQuery, bool>(this);
            this.Mediator.RegisterFeature(ProtocolNamespaces.Blocking);
        }

        public async Task<IEnumerable<string>> RetrieveBlockListAsync()
        {
            var iqResp = await this.XmppStream.WriteIqAndReadReponseAsync(new IqGet(new XElement(XNames.blocking_blocklist))).ConfigureAwait(false);
            var blocklist = iqResp.GetContent<Blocklist>();
            return blocklist.Jids;
        }

        public async Task<bool> BlockAsync(string bareJid)
        {
            var iq = new IqSet(new XElement(XNames.blocking_block, new XElement(XNames.blocking_item, new XAttribute("jid", bareJid.ToBareJid()))));
            var iqResp = await this.XmppStream.WriteIqAndReadReponseAsync(iq).ConfigureAwait(false);
            return iqResp.Type == IqType.result;
        }

        public async Task<bool> UnblockAsync(string bareJid)
        {
            var iq = new IqSet(new Unblock(bareJid));
            var iqResp = await this.XmppStream.WriteIqAndReadReponseAsync(iq).ConfigureAwait(false);
            return iqResp.Type == IqType.result;
        }

        public async Task<bool> UnblockAllAsync()
        {
            var iq = new IqSet(new Unblock());
            var iqResp = await this.XmppStream.WriteIqAndReadReponseAsync(iq).ConfigureAwait(false);
            return iqResp.Type == IqType.result;
        }

        Task<IEnumerable<string>> IAsyncQueryHandler<RetrieveBlockListQuery, IEnumerable<string>>.HandleQueryAsync(RetrieveBlockListQuery query)
        {
            return this.RetrieveBlockListAsync();
        }

        Task<bool> IAsyncQueryHandler<BlockQuery, bool>.HandleQueryAsync(BlockQuery query)
        {
            return this.BlockAsync(query.BareJid);
        }

        Task<bool> IAsyncQueryHandler<UnblockAllQuery, bool>.HandleQueryAsync(UnblockAllQuery query)
        {
            return this.UnblockAllAsync();
        }

        Task<bool> IAsyncQueryHandler<UnblockQuery, bool>.HandleQueryAsync(UnblockQuery query)
        {
            return this.UnblockAsync(query.BareJid);
        }
    }
}
