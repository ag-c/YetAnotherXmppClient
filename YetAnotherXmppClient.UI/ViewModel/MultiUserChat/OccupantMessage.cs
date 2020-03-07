namespace YetAnotherXmppClient.UI.ViewModel.MultiUserChat
{
    public class OccupantMessage : RoomMessage
    {
        public string Nickname { get; set; }

        public OccupantMessage(string nickname, string text)
            : base(text)
        {
            this.Nickname = nickname;
        }
    }
}