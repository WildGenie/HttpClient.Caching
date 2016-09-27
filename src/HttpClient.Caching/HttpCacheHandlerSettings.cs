namespace HttpClient.Caching
{
    using System;
    using HttpClient.Caching.Cache;

    public class HttpCacheHandlerSettings
    {
        private GetUtcNow _getUtcNow;

        public HttpCacheHandlerSettings(IContentStore cacheStore = null)
        {
            CacheStore = cacheStore ?? new InMemoryContentStore();
        }

        public IContentStore CacheStore { get; }

        public GetUtcNow GetUtcNow
        {
            get { return _getUtcNow ?? (() => DateTime.UtcNow); }
            set { _getUtcNow = value; }
        }
    }
}