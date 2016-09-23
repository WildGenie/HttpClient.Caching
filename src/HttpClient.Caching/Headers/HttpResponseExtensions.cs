namespace HttpClient.Caching.Headers
{
    using System;
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

        public static void Parse(this HttpHeaders httpHeaders, string headers)
        {
            if (httpHeaders == null)
            {
                throw new ArgumentNullException(nameof(httpHeaders));
            }

            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            foreach (var header in headers.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var indexOfColon = header.IndexOf(":");
                var name = header.Substring(0, indexOfColon);
                var value = header.Substring(indexOfColon + 1).Trim();
                if (!httpHeaders.TryAddWithoutValidation(name, value))
                {
                    throw new InvalidOperationException($"Value {value} for header {name} not acceptable.");
                }
            }
        }
    }
}