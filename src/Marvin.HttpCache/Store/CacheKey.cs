namespace Marvin.HttpCache.Store
{
    /// <summary>
    ///     The cache key - this can vary by:
    ///     - method
    ///     - URI
    ///     - VaryByHeaders
    /// </summary>
    public class CacheKey
    {
        public CacheKey(string primaryCacheKey)
        {
            // create a new cachekey from the response.  Do check the VaryByHeaders => dictionary?  
            // Should list all the headers defined in the vary by headers value.  
            //
            // Need fast lookup - create string key from all this?

            PrimaryKey = primaryCacheKey ?? "";
            SecondaryKey = "";
        }


        public CacheKey(string primaryCacheKey, string secondaryCacheKey) : this(primaryCacheKey)
        {
            // create a new cachekey from the response.  Do check the VaryByHeaders => dictionary?  
            // Should list all the headers defined in the vary by headers value.  
            //
            // Need fast lookup - create string key from all this?

            SecondaryKey = secondaryCacheKey ?? "";
        }

        public string PrimaryKey { get; }

        public string SecondaryKey { get; }

        public string UnifiedKey { get; private set; }

        public override bool Equals(object obj)
        {
            var secondCacheKey = (CacheKey) obj;
            return (secondCacheKey.PrimaryKey == PrimaryKey)
                   && (secondCacheKey.SecondaryKey == SecondaryKey);
        }

        public override int GetHashCode()
        {
            var hash = 13;
            hash = hash*7 + PrimaryKey.GetHashCode();
            hash = hash*7 + SecondaryKey.GetHashCode();
            return hash;
        }
    }
}