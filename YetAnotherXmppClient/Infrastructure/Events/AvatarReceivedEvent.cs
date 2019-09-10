namespace YetAnotherXmppClient.Infrastructure.Events
{
    public class AvatarReceivedEvent : IEvent
    {
        public string BareJid { get; set; }
        public byte[] Bytes { get; set; }
    }
}
