namespace HttpClient.Caching
{
    using System;
    using System.Net.Http;
    using HttpClient.Caching.Cache;

    public class HttpCacheHandlerSettings
    {
        private GetUtcNow _getUtcNow;

        public HttpCacheHandlerSettings(IContentStore cacheStore = null)
        {
            CacheStore = cacheStore ?? new InMemoryContentStore();
        }

        public IContentStore CacheStore { get; }

        public HttpMessageHandler Inner { get; set; }

        public GetUtcNow GetUtcNow
        {
            get { return _getUtcNow ?? (() => DateTime.UtcNow); }
            set { _getUtcNow = value; }
        }
    }
}