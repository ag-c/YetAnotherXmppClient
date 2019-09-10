namespace YetAnotherXmppClient.Infrastructure.Queries
{
    public class RequestSubscriptionQuery : IQuery<bool>
    {
        public string Jid { get; set; }
    }
}
