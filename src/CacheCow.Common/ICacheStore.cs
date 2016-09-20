namespace CacheCow.Common
{
    using System;
    using System.Net.Http;

    public interface ICacheStore : IDisposable
    {
        bool TryGetValue(CacheKey key, out HttpResponseMessage response);
        void AddOrUpdate(CacheKey key, HttpResponseMessage response);
        bool TryRemove(CacheKey key);
        void Clear();
    }
}