using YetAnotherXmppClient.Protocol.Handler;

namespace YetAnotherXmppClient.Infrastructure.Events
{
    public class ChatStateNotificationReceivedEvent : IEvent
    {
        public ChatState State { get; set; }
        public string FullJid { get; set; }
        public string  Thread { get; set; }
    }
}
