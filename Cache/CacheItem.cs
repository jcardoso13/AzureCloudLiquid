namespace CloudLiquid.Azure
{
    public class CacheItem<T>(T value, TimeSpan expiresAfter)
    {
        #region Public Properties

        public T Value { get; } = value;
        internal DateTimeOffset Created { get; } = DateTimeOffset.Now;
        internal TimeSpan ExpiresAfter { get; } = expiresAfter;

        #endregion
    }
}
