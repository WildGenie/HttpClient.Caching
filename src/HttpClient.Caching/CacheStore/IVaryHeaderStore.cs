namespace HttpClient.Caching.CacheStore
{
    using System.Collections.Generic;

    public interface IVaryHeaderStore
    {
        bool TryGetValue(string uri, out IEnumerable<string> headers);

        void AddOrUpdate(string uri, IEnumerable<string> headers);

        bool TryRemove(string uri);

        void Clear();
    }
}