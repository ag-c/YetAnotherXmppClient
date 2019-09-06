using YetAnotherXmppClient.Protocol.Handler;

namespace YetAnotherXmppClient.Infrastructure.Events
{
    public class MessageReceivedEvent : IEvent
    {
        public ChatSession Session { get; }
        public string Text { get; }

        public MessageReceivedEvent(ChatSession session, string text)
        {
            this.Session = session;
            this.Text = text;
        }
    }
}
