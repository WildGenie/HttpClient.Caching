namespace Marvin.HttpCache.Store
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface ICacheStore
    {
        Task Clear();

        Task<IEnumerable<CacheEntry>> Get(string primaryKey);

        Task<CacheEntry> Get(CacheKey key);

        Task Set(CacheKey key, CacheEntry value);

        Task Remove(CacheKey key);

        Task RemoveRange(string primaryKeyStartsWith);
    }
}