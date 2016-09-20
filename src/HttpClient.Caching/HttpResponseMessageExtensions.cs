namespace HttpClient.Caching
{
    using System.Net.Http;

    internal static class HttpResponseMessageExtensions
    {
        public static HttpResponseMessage AddCacheCowHeader(this HttpResponseMessage response, CacheCowHeader header)
        {
            response.Headers.Add(CacheCowHeader.Name, header.ToString());
            return response;
        }
    }
}