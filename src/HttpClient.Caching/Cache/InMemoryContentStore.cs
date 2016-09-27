namespace HttpClient.Caching.Cache
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;

    public class InMemoryContentStore : IContentStore
    {
        private readonly ConcurrentDictionary<CacheKey, CacheEntryContainer> _cacheContainers = new ConcurrentDictionary<CacheKey, CacheEntryContainer>();
        private readonly ConcurrentDictionary<Guid, HttpResponseMessage> _responseCache = new ConcurrentDictionary<Guid, HttpResponseMessage>();
        
        public async Task<CacheEntry[]> GetEntries(CacheKey cacheKey)
        {
            return _cacheContainers.ContainsKey(cacheKey) 
                ? _cacheContainers[cacheKey].GetEntries()
                : new CacheEntry[0];
        }

        public async Task<HttpResponseMessage> GetResponse(Guid variantId)
        {
            return await CloneResponse(_responseCache[variantId]).ConfigureAwait(false);
        }

        public async Task AddEntry(CacheEntry entry, HttpResponseMessage response)
        {
            var cacheEntryContainer = GetOrCreateContainer(entry.Key);
            cacheEntryContainer.Add(entry);
            _responseCache[entry.VariantId] = response;
        }

        public async Task UpdateEntry(CacheEntry entry, HttpResponseMessage response)
        {
            var cacheEntryContainer = GetOrCreateContainer(entry.Key);
            cacheEntryContainer.Update(entry);
            _responseCache[entry.VariantId] = response;
        }

        private CacheEntryContainer GetOrCreateContainer(CacheKey key)
        {
            return _cacheContainers.GetOrAdd(key, k => new CacheEntryContainer(k));
        }

        private async Task<HttpResponseMessage> CloneResponse(HttpResponseMessage response)
        {
            var newResponse = new HttpResponseMessage(response.StatusCode);
            var ms = new MemoryStream();

            foreach (var v in response.Headers)
            {
                newResponse.Headers.TryAddWithoutValidation(v.Key, v.Value);
            }

            if (response.Content != null)
            {
                await response.Content.CopyToAsync(ms).ConfigureAwait(false);
                ms.Position = 0;
                newResponse.Content = new StreamContent(ms);
                foreach (var v in response.Content.Headers)
                {
                    newResponse.Content.Headers.TryAddWithoutValidation(v.Key, v.Value);
                }
            }
            return newResponse;
        }
    }
}