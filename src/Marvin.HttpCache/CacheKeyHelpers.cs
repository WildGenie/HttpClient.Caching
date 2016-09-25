namespace Marvin.HttpCache
{
    using System.Net.Http;
    using Marvin.HttpCache.Store;

    internal static class CacheKeyHelpers
    {
        internal static string CreatePrimaryCacheKey(HttpRequestMessage request)
        {
            return request.RequestUri.ToString().ToLower();
        }


        internal static CacheKey CreateCacheKey(string primaryCacheKey, string secondaryCacheKey)
        {
            return new CacheKey(primaryCacheKey, secondaryCacheKey);
        }


        internal static CacheKey CreateCacheKey(string primaryCacheKey)
        {
            return new CacheKey(primaryCacheKey, null);
        }
    }
}