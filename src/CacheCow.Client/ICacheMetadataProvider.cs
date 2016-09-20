namespace CacheCow.Client
{
    using System.Collections.Generic;

    public interface ICacheMetadataProvider
    {
        IDictionary<string, long> GetDomainSizes();
        CacheItemMetadata GetEarliestAccessedItem(string domain);
        CacheItemMetadata GetEarliestAccessedItem();
    }
}