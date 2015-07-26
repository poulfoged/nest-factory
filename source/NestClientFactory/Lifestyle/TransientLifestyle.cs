using System;
using System.Collections.Concurrent;

namespace NestClientFactory.Lifestyle
{
    /// <summary>
    /// A life-style where each registration follows this objects lifestyle
    /// </summary>
    public class TransientLifestyle : ILifestyle
    {
        private readonly ConcurrentDictionary<string, object> InitializationStatus = new ConcurrentDictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

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