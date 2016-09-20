namespace CacheCow.Client.Internal
{
    using System.Net.Http;
    using CacheCow.Client.Headers;

    internal static class HttpResponseMessageExtensions
    {
        public static HttpResponseMessage AddCacheCowHeader(this HttpResponseMessage response,
            CacheCowHeader header)
        {
            response.Headers.Add(CacheCowHeader.Name, header.ToString());
            return response;
        }
    }
}