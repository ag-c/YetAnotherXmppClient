namespace YetAnotherXmppClient.Extensions
{
    public static class StringExtensions
    {
        public static string ToBareJid(this string jid)
        {
            if (jid.Contains("/"))
            {
                return jid.Substring(0, jid.IndexOf('/'));
            }

            return jid;
        }
    }
}
