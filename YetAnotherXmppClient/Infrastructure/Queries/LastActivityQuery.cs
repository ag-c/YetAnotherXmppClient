using YetAnotherXmppClient.Protocol.Handler;

namespace YetAnotherXmppClient.Infrastructure.Queries
{
    public class LastActivityQuery : IQuery<LastActivityInfo>
    {
        public string Jid { get; set; }

        public LastActivityQuery(string jid)
        {
            this.Jid = jid;
        }
    }
}
