namespace HttpClient.Caching.Attempt2
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using HttpClient.Caching.Attempt2.CacheStore;
    using Microsoft.IO;

    public class InMemoryCacheStore2 : ICacheStore2
    {
        private static readonly RecyclableMemoryStreamManager RecyclableMemoryStreamManager
            = new RecyclableMemoryStreamManager();

        private ConcurrentDictionary<CacheKey2, CacheItem> _cache 
            = new ConcurrentDictionary<CacheKey2, CacheItem>(CacheKey2.EqualityComparer);

        public async Task AddOrUpdate(CacheKey2 key, HttpRequestMessage request, HttpResponseMessage response)
        {
            var requestStream = RecyclableMemoryStreamManager.GetStream();
            await MessageContentSerializer.Serialize(request, requestStream);

            var responseStream = RecyclableMemoryStreamManager.GetStream();
            await MessageContentSerializer.Serialize(response, responseStream);

            var cacheItem = new CacheItem(requestStream, responseStream);
            _cache.AddOrUpdate(key, _ => cacheItem, (_, __) => cacheItem);
        }

        public Task<CacheEntry> Get(CacheKey2 key)
        {
            CacheItem cacheItem;
            if (!_cache.TryGetValue(key, out cacheItem))
            {
                return Task.FromResult((CacheEntry)null);
            }

            Func<Task<HttpRequestMessage>> getRequest 
                = () => MessageContentSerializer.DeserializeToRequest(cacheItem.Request);
            Func<Task<HttpResponseMessage>> getResponse 
                = () => MessageContentSerializer.DeserializeToResponse(cacheItem.Response);

            return Task.FromResult(new CacheEntry(key, getRequest, getResponse));
        }

        public Task Remove(CacheKey2 key)
        {
            CacheItem cacheItem;
            if (_cache.TryRemove(key, out cacheItem))
            {
                cacheItem.Dispose();
            }
            return Task.CompletedTask;
        }

        public Task Clear()
        {
            var cache = _cache;
            _cache = new ConcurrentDictionary<CacheKey2, CacheItem>();
            var items = cache.Values.ToArray();
            foreach (var cacheItem in items)
            {
                cacheItem.Dispose();
            }
            _cache.Clear();
            return Task.CompletedTask;
        }

        private class CacheItem : IDisposable
        {
            public CacheItem(Stream request, Stream response)
            {
                Request = request;
                Response = response;
            }

            public Stream Request { get; }

            public Stream Response { get; }

            public void Dispose()
            {
                Request.Dispose();
                Response.Dispose();
                GC.SuppressFinalize(this);
            }

            ~CacheItem()
            {
                Request.Dispose();
                Response.Dispose();
            }
        }
    }
}