using System.Collections.Generic;
using YetAnotherXmppClient.Protocol.Handler;

namespace YetAnotherXmppClient.Infrastructure.Events
{
    public class RosterUpdateEvent : IEvent
    {
        public IEnumerable<RosterItem> Items { get; }

        public RosterUpdateEvent(IEnumerable<RosterItem> items)
        {
            this.Items = items;
        }
    }
}