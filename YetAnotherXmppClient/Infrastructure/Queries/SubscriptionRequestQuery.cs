namespace YetAnotherXmppClient.Infrastructure.Queries
{
    public class SubscriptionRequestQuery/*Received*/ : IQuery<bool>
    {
        public string BareJid { get; }

        public SubscriptionRequestQuery(string bareJid)
        {
            this.BareJid = bareJid;
        }
    }
}
