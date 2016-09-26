namespace HttpClient.Caching.Attempt2
{
    using System;

    public class CachingHandler2Settings
    {
        private GetUtcNow _getUtcNow;

        public CachingHandler2Settings(ICacheStore2 cacheStore = null)
        {
            CacheStore = cacheStore ?? new InMemoryCacheStore2();
        }

        public ICacheStore2 CacheStore { get; }

        public GetUtcNow GetUtcNow
        {
            get { return _getUtcNow ?? (() => DateTime.UtcNow); }
            set { _getUtcNow = value; }
        }
    }
}