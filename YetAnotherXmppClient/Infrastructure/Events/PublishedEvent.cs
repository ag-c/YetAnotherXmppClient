using YetAnotherXmppClient.Protocol.Handler;

namespace YetAnotherXmppClient.Infrastructure.Events
{
    public class PublishedEvent : IEvent
    {
        public PubSubItems Items { get; set; }
    }
}
