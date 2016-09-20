namespace CacheCow.Common
{
    using System;

    /// <summary>
    ///     This is an interface representing an ETag store acting similar to a dictionary.
    ///     storing and retriving ETags.
    ///     In a single-server scenario, this could be an in-memory disctionary implementation
    ///     while in a server farm, this will be a persistent store.
    /// </summary>
    public interface IEntityTagStore : IDisposable
    {
        bool TryGetValue(CacheKey key, out TimedEntityTagHeaderValue eTag);
        void AddOrUpdate(CacheKey key, TimedEntityTagHeaderValue eTag);
        int RemoveResource(string resourceUri);
        bool TryRemove(CacheKey key);
        int RemoveAllByRoutePattern(string routePattern);
        void Clear();
    }
}