namespace YetAnotherXmppClient.Infrastructure.Queries
{
    public class UnblockQuery : IQuery<bool>
    {
        public string BareJid { get; set; }
    }
}
