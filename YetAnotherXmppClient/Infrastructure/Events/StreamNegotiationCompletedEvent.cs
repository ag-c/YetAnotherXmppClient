namespace YetAnotherXmppClient.Infrastructure.Events
{
    public class StreamNegotiationCompletedEvent : IEvent
    {
        public string ConnectedJid { get; set; }
    }
}
