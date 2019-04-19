using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YetAnotherXmppClient.Extensions
{
    static class IEnumerableExtensions
    {
        public static IEnumerable<T> Without<T>(this IEnumerable<T> enumerable, T elem)
        {
            return enumerable.Where(f => !f.Equals(elem));
        }
    }
}
