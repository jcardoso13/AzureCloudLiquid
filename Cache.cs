using System;
using System.Collections.Generic;


namespace Azure.Liquid
{
    public class Cache<TKey, TValue> where TKey : notnull
    {
        private static readonly Dictionary<TKey, CacheItem<TValue>> _cache = new Dictionary<TKey, CacheItem<TValue>>();

        public void Store(TKey key, TValue value, TimeSpan expiresAfter)
        {
            _cache[key] = new CacheItem<TValue>(value, expiresAfter);
        }

        public TValue? Get(TKey key)

        {
            if (!_cache.ContainsKey(key))
                return default(TValue);

            var cached = _cache[key];
            if (DateTimeOffset.Now - cached.Created >= cached.ExpiresAfter)
            {
                _cache.Remove(key);
                return default(TValue);
            }

            return cached.Value;
        }
    }

    public class CacheItem<T>
    {
        public CacheItem(T value, TimeSpan expiresAfter)
        {
            Value = value;
            ExpiresAfter = expiresAfter;
        }

        public T Value { get; }
        internal DateTimeOffset Created { get; } = DateTimeOffset.Now;
        internal TimeSpan ExpiresAfter { get; }
    }
}
// Example usage:
// var cache = new Cache<int, string>();
// cache.Store(1, "SomeString", TimeSpan.FromMinutes(2));
// string cachedValue = cache.Get(1);
// Console.WriteLine($"Cached value: {cachedValue}");