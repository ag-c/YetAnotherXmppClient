using System;

namespace YetAnotherXmppClient.Core
{
    interface IIqFactory
    {
        Iq CreateSetIq(object content);
        Iq CreateGetIq(object content);
    }

    class DefaultClientIqFactory : IIqFactory
    {
        private readonly Func<string> fromFunc;

        public DefaultClientIqFactory(Func<string> fromFunc)
        {
            this.fromFunc = fromFunc;
        }

        public Iq CreateSetIq(object content)
        {
            return this.CreateInternal(IqType.set, content);
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
