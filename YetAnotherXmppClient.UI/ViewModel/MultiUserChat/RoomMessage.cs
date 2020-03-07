using System;

namespace YetAnotherXmppClient.UI.ViewModel.MultiUserChat
{
    public class RoomMessage
    {
        public DateTime Time { get; set; }
        public string Text { get; set; }

        public RoomMessage(string text, DateTime time = default)
        {
            this.Time = time == default ? DateTime.Now : time;
            this.Text = text;
        }
    }
}