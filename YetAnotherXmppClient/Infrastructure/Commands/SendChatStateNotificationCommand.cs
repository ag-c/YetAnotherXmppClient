using YetAnotherXmppClient.Protocol.Handler;

namespace YetAnotherXmppClient.Infrastructure.Queries
{
    public class SendChatStateNotificationCommand : ICommand
    {
        public ChatState State { get; set; }
        public string FullJid { get; set; }
        public string Thread { get; set; }
    }
}
