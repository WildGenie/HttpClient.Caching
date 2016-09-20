namespace HttpClient.Caching
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;

    public interface ICacheStore : IDisposable
    {
        Task<bool> TryGetValue(CacheKey key, out HttpResponseMessage response);

        Task AddOrUpdate(CacheKey key, HttpResponseMessage response);

        Task<bool> TryRemove(CacheKey key);

        Task Clear();
    }
}