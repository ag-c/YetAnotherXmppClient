using YetAnotherXmppClient.Core.StanzaParts;

namespace YetAnotherXmppClient.Infrastructure.Commands
{
    public class BroadcastPresenceCommand : ICommand
    {
        public PresenceShow? Show { get; set; }
        public string Status { get; set; }
    }
}
