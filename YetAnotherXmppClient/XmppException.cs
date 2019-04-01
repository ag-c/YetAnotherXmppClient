using System;

namespace YetAnotherXmppClient
{
    public class XmppException : Exception
    {
        public XmppException(string message)
            : base(message)
        {
        }
    }
}
