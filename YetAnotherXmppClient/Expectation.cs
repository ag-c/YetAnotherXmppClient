using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace YetAnotherXmppClient
{
    internal class Expectation
    {
        public static void Expect(string expectation, string actual, [CallerMemberName] string callerMemberName = "")
        {
            if (expectation != actual)
            {
                throw new NotExpectedProtocolException(actual, expectation, callerMemberName);
            }
        }

        public static void Expect(string expectation, string actual, object context, [CallerMemberName] string callerMemberName = "")
        {
            if (expectation != actual)
            {
                throw new NotExpectedProtocolException(actual.ToString(), expectation.ToString(), context, callerMemberName);
            }
        }

        public static void Expect(XName expectation, XName actual, object context, [CallerMemberName] string callerMemberName = "")
        {
            if (expectation != actual)
            {
                throw new NotExpectedProtocolException(actual.ToString(), expectation.ToString(), context, callerMemberName);
            }
        }

        public static void Expect(Expression<Func<bool>> conditionExpr, [CallerMemberName] string callerMemberName = "")
        {
            var conditionFunc = conditionExpr.Compile();
            if (!conditionFunc())
            {
                throw new NotExpectedProtocolException($"false=='{conditionExpr.Body}'", $"true == '{conditionExpr.Body}'", callerMemberName);
            }
        }

        public static void Expect(Expression<Func<bool>> conditionExpr, object context, [CallerMemberName] string callerMemberName = "")
        {
            var conditionFunc = conditionExpr.Compile();
            if (!conditionFunc())
            {
                throw new NotExpectedProtocolException($"false=='{conditionExpr.Body}'", $"true == '{conditionExpr.Body}'", context, callerMemberName);
            }
        }
    }
}