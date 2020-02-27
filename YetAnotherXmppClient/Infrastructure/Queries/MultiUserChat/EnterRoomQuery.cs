using YetAnotherXmppClient.Protocol.Handler.MultiUserChat;

namespace YetAnotherXmppClient.Infrastructure.Queries.MultiUserChat
{
    public class EnterRoomQuery : IQuery<Room>
    {
        public string RoomJid { get; set; }
        public string Nickname { get; set; }

        public EnterRoomQuery(string roomJid, string nickname)
        {
            this.RoomJid = roomJid;
            this.Nickname = nickname;
        }
    }
}
