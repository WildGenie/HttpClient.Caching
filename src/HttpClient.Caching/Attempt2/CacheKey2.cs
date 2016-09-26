namespace HttpClient.Caching.Attempt2
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;

    public class CacheKey2
    {
        public CacheKey2(HttpMethod httpMethod, Uri uri)
        {
            if (httpMethod == null) throw new ArgumentNullException(nameof(httpMethod));
            if (uri == null) throw new ArgumentNullException(nameof(uri));

            Uri = uri;
            HttpMethod = httpMethod;
        }

        public static IEqualityComparer<CacheKey2> EqualityComparer { get; } = new UriHttpMethodEqualityComparer();

        public Uri Uri { get; }

        public HttpMethod HttpMethod { get; }

        private sealed class UriHttpMethodEqualityComparer : IEqualityComparer<CacheKey2>
        {
            public bool Equals(CacheKey2 x, CacheKey2 y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Uri.Equals(y.Uri) && x.HttpMethod.Equals(y.HttpMethod);
            }

            public int GetHashCode(CacheKey2 obj)
            {
                unchecked
                {
                    return (obj.Uri.GetHashCode()*397) ^ obj.HttpMethod.GetHashCode();
                }
            }
        }
    }
}