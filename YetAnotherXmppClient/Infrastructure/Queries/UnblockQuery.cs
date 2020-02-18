namespace YetAnotherXmppClient.Infrastructure.Queries
{
    public class UnblockQuery : IQuery<bool>
    {
        public string BareJid { get; }

        public UnblockQuery(string bareJid)
        {
            this.BareJid = bareJid;
        }
    }
}
