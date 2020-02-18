using System.Collections.Generic;
using YetAnotherXmppClient.Protocol.Handler;

namespace YetAnotherXmppClient.Infrastructure.Queries
{
    public class PresencesQuery : IQuery<IEnumerable<Presence>>
    {
        public string BareJid { get; set; }

        public PresencesQuery(string bareJid)
        {
            this.BareJid = bareJid;
        }
    }
}
