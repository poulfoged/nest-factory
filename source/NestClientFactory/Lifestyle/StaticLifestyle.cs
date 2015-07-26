using System;
using System.Collections.Concurrent;

namespace NestClientFactory.Lifestyle
{
    /// <summary>
    /// A lifestyle that stores it's status in a static dictionary
    /// </summary>
    public class StaticLifestyle : ILifestyle
    {
        private static readonly ConcurrentDictionary<string, object> InitializationStatus = new ConcurrentDictionary<string, object>(StringComparer.InvariantCultureIgnoreCase); 

        public T TryGet<T>(string key)
        {
            object arg;
            InitializationStatus.TryGetValue(key, out arg);
            return (T)arg;
        }

        public bool TryAdd<T>(string key, T arg)
        {
            return InitializationStatus.TryAdd(key, arg);
        }
    }
}