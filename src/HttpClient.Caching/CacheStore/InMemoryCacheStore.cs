namespace HttpClient.Caching.CacheStore
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Runtime.Caching;
    using System.Threading.Tasks;

    public class InMemoryCacheStore : ICacheStore
    {
        private const string CacheStoreEntryName = "###InMemoryCacheStore_###";
        private static readonly TimeSpan DefaultCacheExpiry = TimeSpan.FromHours(6);
        private readonly TimeSpan _defaultExpiry;

        private readonly MessageContentHttpMessageSerializer _messageSerializer =
            new MessageContentHttpMessageSerializer(true);

        private MemoryCache _responseCache = new MemoryCache(CacheStoreEntryName);

        public InMemoryCacheStore()
            : this(DefaultCacheExpiry)
        {}

        public InMemoryCacheStore(TimeSpan defaultExpiry)
        {
            _defaultExpiry = defaultExpiry;
        }

        public Task<bool> TryGetValue(CacheKey key, out HttpResponseMessage response)
        {
            response = null;
            var result = _responseCache.Get(key.HashBase64);
            if(result != null)
                response = _messageSerializer.DeserializeToResponseAsync(new MemoryStream((byte[]) result)).Result;
            return Task.FromResult(result != null);
        }

        public async Task AddOrUpdate(CacheKey key, HttpResponseMessage response)
        {
            // removing reference to request so that the request can get GCed
            var req = response.RequestMessage;
            response.RequestMessage = null;
            var memoryStream = new MemoryStream();
            await _messageSerializer.SerializeAsync(response, memoryStream);
            response.RequestMessage = req;
            _responseCache.Set(key.HashBase64, memoryStream.ToArray(), GetExpiry(response));
        }

        public Task<bool> TryRemove(CacheKey key)
        {
            return Task.FromResult(_responseCache.Remove(key.HashBase64) != null);
        }

        public Task Clear()
        {
            _responseCache.Dispose();
            _responseCache = new MemoryCache(CacheStoreEntryName);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _responseCache.Dispose();
        }

        private DateTimeOffset GetExpiry(HttpResponseMessage response)
        {
            if(response.Content == null)
                return (response.Headers.CacheControl != null) && response.Headers.CacheControl.MaxAge.HasValue
                    ? DateTimeOffset.UtcNow.Add(response.Headers.CacheControl.MaxAge.Value)
                    : DateTimeOffset.UtcNow.Add(_defaultExpiry);
            return response.Content.Headers.Expires.HasValue
                ? response.Content.Headers.Expires.Value
                : DateTimeOffset.UtcNow.Add(_defaultExpiry);
        }
    }
}