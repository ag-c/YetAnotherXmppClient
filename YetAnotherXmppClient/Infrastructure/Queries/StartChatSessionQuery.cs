using YetAnotherXmppClient.Protocol.Handler;

namespace YetAnotherXmppClient.Infrastructure.Queries
{
    public class StartChatSessionQuery : IQuery<ChatSession>
    {
        public string Jid { get; }

        public StartChatSessionQuery(string jid)
        {
            this.Jid = jid;
        }
    }
}
