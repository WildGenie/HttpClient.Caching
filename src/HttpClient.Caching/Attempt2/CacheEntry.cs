namespace HttpClient.Caching.Attempt2
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;

    public class CacheEntry
    {
        private readonly Func<Task<HttpRequestMessage>> _getCachedRequest;
        private readonly Func<Task<HttpResponseMessage>> _getCachedResponse;

        public CacheEntry(CacheKey2 key,
            Func<Task<HttpRequestMessage>> getCachedRequest,
            Func<Task<HttpResponseMessage>> getCachedResponse)
        {
            Key = key;
            _getCachedRequest = getCachedRequest;
            _getCachedResponse = getCachedResponse;
        }

        public CacheKey2 Key { get; }

        public Task<HttpRequestMessage> GetCachedRequest()
        {
            return _getCachedRequest();
        }

        public Task<HttpResponseMessage> GetCachedResponse()
        {
            return _getCachedResponse();
        }
    }
}