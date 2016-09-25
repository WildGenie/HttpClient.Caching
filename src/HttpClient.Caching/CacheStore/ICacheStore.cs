namespace HttpClient.Caching.CacheStore
{
    using System.Net.Http;
    using System.Threading.Tasks;

    public interface ICacheStore
    {
        Task<HttpResponseMessage> TryGetValue(CacheKey key);

        Task AddOrUpdate(CacheKey key, HttpResponseMessage response);

        Task<bool> TryRemove(CacheKey key);

        Task Clear();
    }
}