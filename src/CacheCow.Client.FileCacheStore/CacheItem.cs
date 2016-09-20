namespace CacheCow.Client.FileCacheStore
{
    using System;

    internal class CacheItem
    {
        public string Hash { get; set; }
        public string Domain { get; set; }
        public long Size { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime LastAccessed { get; set; }
        private CacheItemMetadata _metadata { get; set; }

        public CacheItemMetadata Metadata
        {
            get
            {
                if(_metadata == null)
                    _metadata = new CacheItemMetadata
                    {
                        Domain = Domain,
                        Key = Convert.FromBase64String(Hash),
                        LastAccessed = LastAccessed,
                        Size = Size
                    };
                return _metadata;
            }
        }
    }
}