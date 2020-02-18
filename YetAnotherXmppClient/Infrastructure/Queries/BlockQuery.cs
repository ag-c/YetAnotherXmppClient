namespace YetAnotherXmppClient.Infrastructure.Queries
{
    public class BlockQuery : IQuery<bool>
    {
        public string BareJid { get; set; }

        public BlockQuery(string bareJid)
        {
            this.BareJid = bareJid;
        }
    }
}
