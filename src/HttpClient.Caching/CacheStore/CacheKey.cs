namespace HttpClient.Caching.CacheStore
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text;

    public class CacheKey
    {
        private const string CacheKeyFormat = "{0}-{1}";
        private readonly string _toString;
        private string _domain;

        public CacheKey(string resourceUri, IEnumerable<string> headerValues)
            : this(resourceUri, headerValues, resourceUri)
        {}

        private CacheKey(string resourceUri, IEnumerable<string> headerValues, string routePattern)
        {
            RoutePattern = routePattern;

            _toString = string.Format(CacheKeyFormat, resourceUri, string.Join("-", headerValues));
            using(var sha1 = new SHA1CryptoServiceProvider())
            {
                Hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(_toString));
            }
            HashBase64 = Convert.ToBase64String(Hash);

            ResourceUri = resourceUri;

            // Starting with v0.5, query string parameters are removed from the resourceUri
            var indexOfQuestionMark = ResourceUri.IndexOf('?');
            if(indexOfQuestionMark > 0)
                ResourceUri = ResourceUri.Substring(0, indexOfQuestionMark);
        }


        public string ResourceUri { get; }

        public string RoutePattern { get; }

        public byte[] Hash { get; }

        public string HashBase64 { get; }

        public string Domain => _domain ?? (_domain = new Uri(ResourceUri).Host);

        public override string ToString()
        {
            return _toString;
        }

        public override bool Equals(object obj)
        {
            var eTagKey = obj as CacheKey;
            if(eTagKey == null)
            {
                return false;
            }
            return ToString() == eTagKey.ToString();
        }

        public override int GetHashCode()
        {
            return _toString.GetHashCode();
        }
    }
}