using System;
using System.Collections.Concurrent;

namespace YetAnotherXmppClient.Extensions
{
    static class ConcurrentDictionaryExtensions
    {
        public static void AddAndUpdate<TKey, T>(this ConcurrentDictionary<TKey, T> q, TKey key, Action<T> updateAction) where T : class, new()
        {
            q.AddOrUpdate(key, _ =>
            {
                var obj = new T();
                updateAction(obj);
                return obj;
            }, (_, existing) =>
            {
                updateAction(existing);
                return existing;
            });
        }
    }
}
