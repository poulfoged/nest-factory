using System;
using System.Collections.Concurrent;

namespace NestClientFactory.Lifestyle
{
    /// <summary>
    /// A life-style that does not store anything
    /// </summary>
    public class NullLifeStyle : ILifestyle
    {
        public T TryGet<T>(string key)
        {
            return default(T);
        }

        public bool TryAdd<T>(string key, T arg)
        {
            return true;
        }
    }
}