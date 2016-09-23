namespace HttpClient.Caching.Headers
{
    using System;
    using System.Reflection;
    using System.Text;

    public class CachingHeader
    {
        // NOTE: public const has problems such as 
        // if changed, all dependent libraries have to be recompiled
        // but it has the advantage of using at compile-time
        // so conscious decision to use const instead of static readonly

        public const string Name = "x-cachecow";

        public bool? WasStale { get; set; }

        public bool? DidNotExist { get; set; }

        public bool? NotCacheable { get; set; }

        public bool? CacheValidationApplied { get; set; }

        public bool? RetrievedFromCache { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            AddToStringBuilder(sb, WasStale, ExtensionNames.WasStale);
            AddToStringBuilder(sb, NotCacheable, ExtensionNames.NotCacheable);
            AddToStringBuilder(sb, DidNotExist, ExtensionNames.DidNotExist);
            AddToStringBuilder(sb, CacheValidationApplied, ExtensionNames.CacheValidationApplied);
            AddToStringBuilder(sb, RetrievedFromCache, ExtensionNames.RetrievedFromCache);
            return sb.ToString();
        }

        private void AddToStringBuilder(StringBuilder sb, bool? property, string extensionName)
        {
            if (property != null)
            {
                sb.Append(';');
                sb.Append(extensionName);
                sb.Append('=');
                sb.Append(property.Value.ToString().ToLower());
            }
        }

        public static bool TryParse(string value, out CachingHeader cachingHeader)
        {
            cachingHeader = null;

            if (value == null)
            {
                return false;
            }

            if (value == string.Empty)
            {
                return false;
            }

            cachingHeader = new CachingHeader();
            var chunks = value.Split(new[] {";"}, StringSplitOptions.None);

            foreach (var chunk in chunks)
            {
                cachingHeader.WasStale = cachingHeader.WasStale ?? ParseNameValue(chunk, ExtensionNames.WasStale);
                cachingHeader.CacheValidationApplied = cachingHeader.CacheValidationApplied ??
                                                       ParseNameValue(chunk, ExtensionNames.CacheValidationApplied);
                cachingHeader.NotCacheable = cachingHeader.NotCacheable ??
                                             ParseNameValue(chunk, ExtensionNames.NotCacheable);
                cachingHeader.DidNotExist = cachingHeader.DidNotExist ??
                                            ParseNameValue(chunk, ExtensionNames.DidNotExist);
                cachingHeader.RetrievedFromCache = cachingHeader.RetrievedFromCache ??
                                                   ParseNameValue(chunk, ExtensionNames.RetrievedFromCache);
            }

            return true;
        }

        private static bool? ParseNameValue(string entry, string name)
        {
            if (string.IsNullOrEmpty(entry))
                return null;

            var chunks = entry.Split('=');
            if (chunks.Length != 2)
            {
                return null;
            }

            chunks[0] = chunks[0].Trim();
            chunks[1] = chunks[1].Trim();

            if (chunks[0].ToLower() != name)
            {
                return null;
            }

            var result = false;
            if (!bool.TryParse(chunks[1], out result))
            {
                return null;
            }

            return result;
        }

        public static class ExtensionNames
        {
            public const string WasStale = "was-stale";
            public const string DidNotExist = "did-not-exist";
            public const string NotCacheable = "not-cacheable";
            public const string CacheValidationApplied = "cache-validation-applied";
            public const string RetrievedFromCache = "retrieved-from-cache";
        }
    }
}