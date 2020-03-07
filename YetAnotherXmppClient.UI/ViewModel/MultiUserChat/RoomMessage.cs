using System;

namespace YetAnotherXmppClient.UI.ViewModel.MultiUserChat
{
    public class RoomMessage
    {
        public DateTime Time { get; set; }
        public string Text { get; set; }

        public RoomMessage(string text)
        {
            this.Time = DateTime.Now;
            this.Text = text;
        }
    }
}