// Example usage:
// var cache = new Cache<int, string>();
// cache.Store(1, "SomeString", TimeSpan.FromMinutes(2));
// string cachedValue = cache.Get(1);
// Console.WriteLine($"Cached value: {cachedValue}");

namespace CloudLiquid.Azure
{
    public class Cache<TKey, TValue> where TKey : notnull
    {
        #region Private Members

        private static readonly Dictionary<TKey, CacheItem<TValue>> cache = [];

        #endregion

        #region Public Methods

        public void Store(TKey key, TValue value, TimeSpan expiresAfter)
        {
            cache[key] = new CacheItem<TValue>(value, expiresAfter);
        }

        public TValue? Get(TKey key)

        {
            if (!cache.TryGetValue(key, out CacheItem<TValue>? value))
            {
                return default;
            }

            var cached = value;

            if (DateTimeOffset.Now - cached.Created >= cached.ExpiresAfter)
            {
                cache.Remove(key);
                return default;
            }

            return cached.Value;
        }

        #endregion
    }
}
