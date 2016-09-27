namespace HttpClient.Caching.Cache
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;

    public interface IContentStore
    {
        Task<CacheEntry[]> GetEntries(CacheKey cacheKey);

        Task<HttpResponseMessage> GetResponse(Guid variantId);

        Task AddEntry(CacheEntry entry, HttpResponseMessage response);

        Task UpdateEntry(CacheEntry entry, HttpResponseMessage response);

        Task RemoveEntries(CacheKey cacheKey);
    }
}