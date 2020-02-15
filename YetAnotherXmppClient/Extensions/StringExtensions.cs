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

        public static bool IsBareJid(this string jid)
        {
            if (jid?.Contains('/') ?? true)
                return false;

            var parts = jid?.Split('@');
            
            return parts?.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]) && !string.IsNullOrWhiteSpace(parts[1]);
        }
    }
}
