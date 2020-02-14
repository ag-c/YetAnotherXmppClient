namespace YetAnotherXmppClient.Protocol.Handler.MultiUserChat
{
    public class Room
    {
        public string Jid { get; set; }
        public string Name { get; set; }

        public Room(string jid, string name)
        {
            this.Jid = jid;
            this.Name = name;
        }
    }
}