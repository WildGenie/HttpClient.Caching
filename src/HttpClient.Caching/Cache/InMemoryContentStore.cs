namespace HttpClient.Caching.Cache
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    public class InMemoryContentStore : IContentStore
    {
        private readonly object _syncRoot = new object();
        private readonly ConcurrentDictionary<CacheKey, CacheEntryContainer> _cacheContainers = new ConcurrentDictionary<CacheKey, CacheEntryContainer>();
        private readonly Dictionary<Guid, HttpResponseMessage> _responseCache = new Dictionary<Guid, HttpResponseMessage>();
        
        public async Task<CacheEntry[]> GetEntriesAsync(CacheKey cacheKey)
        {
            return _cacheContainers.ContainsKey(cacheKey) ? _cacheContainers[cacheKey].Entries.ToArray() : new CacheEntry[0];
        }

        public async Task<HttpResponseMessage> GetResponseAsync(Guid variantId)
        {
            return await CloneResponseAsync(_responseCache[variantId]).ConfigureAwait(false);
        }

        public async Task AddEntryAsync(CacheEntry entry, HttpResponseMessage response)
        {
            var cacheEntryContainer = GetOrCreateContainer(entry.Key);
            lock (_syncRoot)
            {
                cacheEntryContainer.Entries.Add(entry);
                _responseCache[entry.VariantId] = response;
            }
        }

        public async Task UpdateEntryAsync(CacheEntry entry, HttpResponseMessage response)
        {
            var cacheEntryContainer = GetOrCreateContainer(entry.Key);
            
            lock (_syncRoot)
            {
                var oldentry = cacheEntryContainer.Entries.First(e => e.VariantId == entry.VariantId);
                cacheEntryContainer.Entries.Remove(oldentry);
                cacheEntryContainer.Entries.Add(entry);
                _responseCache[entry.VariantId] = response;
            }
        }

        private CacheEntryContainer GetOrCreateContainer(CacheKey key)
        {
            return _cacheContainers.GetOrAdd(key, k => new CacheEntryContainer(k));
        }

        private async Task<HttpResponseMessage> CloneResponseAsync(HttpResponseMessage response)
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