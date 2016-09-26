namespace HttpClient.Caching.Attempt2
{
    using System.Net.Http;
    using System.Threading.Tasks;

    public interface ICacheStore2
    {
        Task AddOrUpdate(CacheKey2 key, HttpRequestMessage request, HttpResponseMessage response);

        Task<CacheEntry> Get(CacheKey2 key);

        Task Remove(CacheKey2 key);

        Task Clear();
    }
}