using YetAnotherXmppClient.Core;

namespace YetAnotherXmppClient.Infrastructure.Events
{
    public class PresenceEvent : IEvent
    {
        public Jid Jid { get; set; }

        public bool IsAvailable { get; set; }
    }
}
