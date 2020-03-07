using System;

namespace YetAnotherXmppClient.UI.ViewModel.MultiUserChat
{
    public class OccupantMessage : RoomMessage
    {
        public string Nickname { get; set; }

        public OccupantMessage(string nickname, string text, DateTime time = default)
            : base(text, time)
        {
            this.Nickname = nickname;
        }
    }
}