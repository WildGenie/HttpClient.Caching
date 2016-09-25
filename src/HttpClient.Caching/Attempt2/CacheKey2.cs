namespace HttpClient.Caching.Attempt2
{
    using System;
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

        public Uri Uri { get; }

        public HttpMethod HttpMethod { get; }
    }
}