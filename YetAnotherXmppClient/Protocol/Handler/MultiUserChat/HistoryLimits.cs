using System;

namespace YetAnotherXmppClient.Protocol.Handler.MultiUserChat
{
    public class HistoryLimits
    {
        /// <summary>
        /// Limit the total number of characters in the history to "X"
        /// </summary>
        public int? MaxChars;

        /// <summary>
        /// Limit the total number of messages in the history to "X".
        /// </summary>
        public int? MaxStanzas;

        /// <summary>
        /// Send only the messages received in the last "X" seconds.
        /// </summary>
        public int? Seconds;

        /// <summary>
        /// Send only the messages received since the UTC datetime specified
        /// </summary>
        public DateTime? Since;
    }
}
