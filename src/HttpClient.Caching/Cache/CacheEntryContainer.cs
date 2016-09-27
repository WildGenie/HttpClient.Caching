namespace HttpClient.Caching.Cache
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;

    public class CacheEntryContainer
    {
        private readonly ConcurrentDictionary<Guid, CacheEntry> _entries = new ConcurrentDictionary<Guid, CacheEntry>();

        public CacheEntryContainer(CacheKey primaryCacheKey)
        {
            PrimaryCacheKey = primaryCacheKey;
        }

        public CacheKey PrimaryCacheKey { get; }

        public void Add(CacheEntry entry)
        {
            _entries.TryAdd(entry.VariantId, entry);
        }

        public void Update(CacheEntry entry)
        {
            _entries[entry.VariantId] = entry;
        }

        public CacheEntry[] GetEntries()
        {
            return _entries.Values.ToArray();
        }
    }
}