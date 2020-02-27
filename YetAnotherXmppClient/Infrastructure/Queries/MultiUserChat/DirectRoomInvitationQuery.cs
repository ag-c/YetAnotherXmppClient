namespace YetAnotherXmppClient.Infrastructure.Queries.MultiUserChat
{
    public class DirectRoomInvitationQuery : IQuery<bool>
    {
        public string RoomJid { get; set; }
        public string Reason { get; set; }

        internal DirectRoomInvitationQuery(string roomJid, string reason)
        {
            this.RoomJid = roomJid;
            this.Reason = reason;
        }
    }
}
