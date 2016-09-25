namespace Marvin.HttpCache.Store
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class ImmutableInMemoryCacheStore : ICacheStore
    {
        private IImmutableDictionary<CacheKey, CacheEntry> _cache = ImmutableDictionary.Create<CacheKey, CacheEntry>();

        public Task<IEnumerable<CacheEntry>> Get(string primaryKey)
        {
            primaryKey = primaryKey.ToLower();

            var validCacheKeys = _cache.Keys.Where(k => k.PrimaryKey == primaryKey).ToArray();

            if (validCacheKeys.Any())
            {
                var selectedValues = validCacheKeys.Where(_cache.ContainsKey)
                    .Select(k => _cache[k]);

                return Task.FromResult(selectedValues);
            }
            return Task.FromResult(default(IEnumerable<CacheEntry>));
        }


        public Task<CacheEntry> Get(CacheKey key)
        {
            CacheEntry value;
            return Task.FromResult(_cache.TryGetValue(key, out value) ? value : default(CacheEntry));
        }


        public Task Set(CacheKey key, CacheEntry value)
        {
            do
            {
                var oldCache = _cache;
                IImmutableDictionary<CacheKey, CacheEntry> newCache;

                if (oldCache.ContainsKey(key))
                    newCache = oldCache.SetItem(key, value);
                else
                    newCache = oldCache.Add(key, value);

                // newCache = new cache dic, containing value.  Check.

                // Interlocked.CompareExchange(ref _cache, newCache, oldCache): 
                //
                // => if _cache is the same as oldcache, then  replace
                // _cache by newCache.  This is an effective check: if _cache is no longer the
                // same as oldCache, another thread has made a change to _cache.  If that's the
                // case, we need to do the add again, as we'll want to make sure we always work 
                // on the latest version - we don't want to loose changes to the cache.
                //
                // Call checks for reference equality, not an overridden Equals => we need
                // this reference check, new instance = different reference.
                //
                // CompareExchange always returns the value in "location", eg the first 
                // parameter, BEFORE the exchange.  So, if we check that value (_cache before 
                // exchange) against the oldCache and if these are the same, add was succesful,
                // and thanks to the CompareExchange call, _cache is now set to newCache

                // compares oldCache with newCache - if these are now the s
                if (oldCache == Interlocked.CompareExchange(ref _cache, newCache, oldCache))
                    return Task.FromResult(true);

                // CompareExchange failed => another thread has made a change to _cache.
                // We need to do the add again, as we'll want to make sure we always work 
                // on the latest version - we don't want to loose changes to the cache.
            } while (true);
        }

        public Task Remove(CacheKey key)
        {
            do
            {
                var oldCache = _cache;
                var newCache = oldCache.ContainsKey(key) ? oldCache.Remove(key) : oldCache;

                // compares oldCache with newCache - if these are now the s
                if (oldCache == Interlocked.CompareExchange(ref _cache, newCache, oldCache))
                {
                    return Task.FromResult(true);
                }

                // CompareExchange failed => another thread has made a change to _cache.
                // We need to do the add again, as we'll want to make sure we always work 
                // on the latest version - we don't want to loose changes to the cache.
            } while (true);
        }


        public Task RemoveRange(string primaryKeyStartsWith)
        {
            do
            {
                primaryKeyStartsWith = primaryKeyStartsWith.ToLower();

                var oldCache = _cache;

                var listOfKeys = oldCache.Keys.Where(k => k.PrimaryKey.StartsWith(primaryKeyStartsWith)).ToArray();

                var newCache = listOfKeys.Any() ? oldCache.RemoveRange(listOfKeys) : oldCache;

                // compares oldCache with newCache - if these are now the s
                if (oldCache == Interlocked.CompareExchange(ref _cache, newCache, oldCache))
                    return Task.FromResult(true);

                // CompareExchange failed => another thread has made a change to _cache.
                // We need to do the add again, as we'll want to make sure we always work 
                // on the latest version - we don't want to loose changes to the cache.
            } while (true);
        }

        public Task Clear()
        {
            _cache = _cache.Clear();
            return Task.FromResult(true);
        }
    }
}