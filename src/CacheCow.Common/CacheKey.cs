namespace CacheCow.Common
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

        /// <summary>
        ///     constructor for CacheKey
        /// </summary>
        /// <param name="resourceUri">URI of the resource</param>
        /// <param name="headerValues">
        ///     value of the headers as in the request. Only those values whose named defined in VaryByHeader
        ///     must be passed
        /// </param>
        /// <param name="routePattern">
        ///     route pattern for the URI. by default it is the same
        ///     but in some cases it could be different.
        ///     For example /api/cars/fastest and /api/cars/mostExpensive can share tha pattern /api/cars/*
        ///     This will be used at the time of cache invalidation.
        /// </param>
        public CacheKey(string resourceUri, IEnumerable<string> headerValues, string routePattern)
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

        public string Domain
        {
            get
            {
                if(_domain == null)
                    _domain = new Uri(ResourceUri).Host;
                return _domain;
            }
        }

        public override string ToString()
        {
            return _toString;
        }


        public override bool Equals(object obj)
        {
            if(obj == null)
                return false;
            var eTagKey = obj as CacheKey;
            if(eTagKey == null)
                return false;
            return ToString() == eTagKey.ToString();
        }

        public override int GetHashCode()
        {
            return _toString.GetHashCode();
        }
    }
}