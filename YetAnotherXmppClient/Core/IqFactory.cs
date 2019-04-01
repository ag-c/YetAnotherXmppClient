using System;
using YetAnotherXmppClient.Core.Stanza;

namespace YetAnotherXmppClient.Core
{
    interface IIqFactory
    {
        Iq CreateSetIq(object content, string from = null);
        Iq CreateGetIq(object content);
    }

    class DefaultClientIqFactory : IIqFactory
    {
        private readonly Func<string> fromFunc;

        public DefaultClientIqFactory(Func<string> fromFunc)
        {
            this.fromFunc = fromFunc;
        }

        public Iq CreateSetIq(object content, string from = null)
        {
            var iq = this.CreateInternal(IqType.set, content);
            if (from != null)
                iq.From = from;
            return iq;
        }

        public Iq CreateGetIq(object content)
        {
            return this.CreateInternal(IqType.get, content);
        }

        private Iq CreateInternal(IqType iqType, object content)
        {
            var iq = new Iq(iqType, content);
            if (this.fromFunc != null)
                iq.From = this.fromFunc();
            return iq;
        }
    }
}
