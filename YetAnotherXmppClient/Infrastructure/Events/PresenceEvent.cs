using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.StanzaParts;

namespace YetAnotherXmppClient.Infrastructure.Events
{
    public class PresenceEvent : IEvent
    {
        public Jid Jid { get; set; }

        public bool IsAvailable { get; set; }

        public PresenceShow? Show { get; set; }
    }
}
