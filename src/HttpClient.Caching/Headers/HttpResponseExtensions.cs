namespace HttpClient.Caching.Headers
{
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;

    public static class HttpResponseExtensions
    {
        internal static HttpResponseMessage AddCachingHeader(this HttpResponseMessage response, CachingHeader header)
        {
            response.Headers.Add(CachingHeader.Name, header.ToString());
            return response;
        }

        public static CachingHeader GetCachingHeader(this HttpResponseHeaders headers)
        {
            if (headers == null)
            {
                return null;
            }
            CachingHeader header = null;
            var cacheCowHeader = headers.FirstOrDefault(x => x.Key == CachingHeader.Name);

            if (cacheCowHeader.Value.Any())
            {
                var last = cacheCowHeader.Value.Last();
                CachingHeader.TryParse(last, out header);
            }

            return header;
        }
    }
}