using System;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using YetAnotherXmppClient.Core.Stanza;

namespace YetAnotherXmppClient.Extensions
{
    public static class IqExtensions
    {
        public static TContentElem GetContent<TContentElem>(this Iq iq)
        {
            var constructorInfo = typeof(TContentElem).GetConstructor(BindingFlags.NonPublic|BindingFlags.Instance, null, new[] { typeof(XElement) }, null);
            if (constructorInfo == null)
            {
                throw new NotSupportedException("Content model type has no private constructor that takes an XElement to clone");
            }
            var contentXElem = iq.Elements().Single();
            return (TContentElem)constructorInfo.Invoke(new object[] { contentXElem });
        }
    }
}
