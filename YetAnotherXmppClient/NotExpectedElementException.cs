using System;
using System.Runtime.CompilerServices;

namespace YetAnotherXmppClient
{
    public class NotExpectedProtocolException : Exception
    {
        public string Actual { get; }
        public string Expected { get; }
        public object Context { get; }
        public string ThrownBy { get; }

        public NotExpectedProtocolException(string actual, string expected, [CallerMemberName] string thrownBy = "")
        {
            Actual = actual;
            Expected = expected;
            ThrownBy = thrownBy;
        }
        
        public NotExpectedProtocolException(string actual, string expected, object context, [CallerMemberName] string thrownBy = "")
        {
            Actual = actual;
            Expected = expected;
            Context = context;
            ThrownBy = thrownBy;
        }
    }
}
