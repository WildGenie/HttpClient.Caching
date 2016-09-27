namespace HttpClient.Caching.Cache
{
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;

    public static class MessageContentSerializer
    {
        public static async Task Serialize(HttpResponseMessage response, Stream destinationStream)
        {
            var httpMessageContent = new HttpMessageContent(response);
            var sourceStream = await httpMessageContent.ReadAsStreamAsync().ConfigureAwait(false);
            await sourceStream.CopyToAsync(destinationStream);
        }

        public static async Task Serialize(HttpRequestMessage request, Stream destinationStream)
        {
            var httpMessageContent = new HttpMessageContent(request);
            var sourceStream = await httpMessageContent.ReadAsStreamAsync().ConfigureAwait(false);
            await sourceStream.CopyToAsync(destinationStream);
        }

        public static Task<HttpResponseMessage> DeserializeToResponse(Stream sourceStream)
        {
            sourceStream.Position = 0;
            var response = new HttpResponseMessage
            {
                Content = new StreamContent(sourceStream)
            };
            response.Content.Headers.Add("Content-Type", "application/http;msgtype=response");
            return response.Content.ReadAsHttpResponseMessageAsync();
        }

        public static Task<HttpRequestMessage> DeserializeToRequest(Stream sourceStream)
        {
            sourceStream.Position = 0;
            var request = new HttpRequestMessage
            {
                Content = new StreamContent(sourceStream)
            };
            request.Content.Headers.Add("Content-Type", "application/http;msgtype=request");
            return request.Content.ReadAsHttpRequestMessageAsync();
        }
    }
}