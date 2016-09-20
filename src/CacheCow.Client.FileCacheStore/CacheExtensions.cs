namespace CacheCow.Client.FileCacheStore
{
    using System.IO;
    using CacheCow.Common;
    using CacheCow.Common.Helpers;

    internal static class CacheExtensions
    {
        public static string EnsureFolderAndGetFileName(this CacheKey key, string dataRoot)
        {
            return EnsureFolderAndGetFileName(key.Domain, key.Hash, dataRoot);
        }

        private static string EnsureFolderAndGetFileName(string domain, byte[] key, string dataRoot)
        {
            var directory = Path.Combine(dataRoot, domain);
            if(!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            var fileName = key.ToHex();
            return Path.Combine(directory, fileName);
        }

        public static string EnsureFolderAndGetFileName(this CacheItemMetadata metadata, string dataRoot)
        {
            return EnsureFolderAndGetFileName(metadata.Domain, metadata.Key, dataRoot);
        }
    }
}