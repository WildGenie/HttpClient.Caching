namespace CacheCow.Client
{
    using System;

    public class CacheItemMetadata
    {
        public byte[] Key { get; set; }
        public DateTime LastAccessed { get; set; }
        public long Size { get; set; }
        public string Domain { get; set; }
    }
}