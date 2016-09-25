namespace HttpClient.Caching.Attempt2.CacheStore
{
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;

    public static class MessageContentSerializer
    {
        public static async Task Serialize(HttpResponseMessage response, Stream stream)
        {
            if(response.Content != null)
            {
                await response.Content.LoadIntoBufferAsync();
            }
            var httpMessageContent = new HttpMessageContent(response);
            var buffer = await httpMessageContent.ReadAsByteArrayAsync().ConfigureAwait(false);
            await stream.WriteAsync(buffer, 0, buffer.Length);
        }

        public static async Task Serialize(HttpRequestMessage request, Stream stream)
        {
            if(request.Content != null)
            {
                await request.Content.LoadIntoBufferAsync();
            }
            var httpMessageContent = new HttpMessageContent(request);
            var buffer = await httpMessageContent.ReadAsByteArrayAsync();
            await stream.WriteAsync(buffer, 0, buffer.Length);
        }

        public static Task<HttpResponseMessage> DeserializeToResponse(Stream stream)
        {
            var response = new HttpResponseMessage
            {
                Content = new StreamContent(stream)
            };
            response.Content.Headers.Add("Content-Type", "application/http;msgtype=response");
            return response.Content.ReadAsHttpResponseMessageAsync();
        }

        public static Task<HttpRequestMessage> DeserializeToRequest(Stream stream)
        {
            var request = new HttpRequestMessage
            {
                Content = new StreamContent(stream)
            };
            request.Content.Headers.Add("Content-Type", "application/http;msgtype=request");
            return request.Content.ReadAsHttpRequestMessageAsync();
        }
    }
}