namespace HttpClient.Caching.CacheStore
{
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    ///     Default implementation of IHttpMessageSerializer using proprietry format
    ///     Does not close the stream since the stream can be used to store other objects
    ///     so it has to be closed in the client
    /// </summary>
    public class MessageContentHttpMessageSerializer : IHttpMessageSerializerAsync
    {
        private bool _bufferContent;

        public MessageContentHttpMessageSerializer()
            : this(false)
        {}

        public MessageContentHttpMessageSerializer(bool bufferContent)
        {
            _bufferContent = bufferContent;
        }

        public async Task Serialize(HttpResponseMessage response, Stream stream)
        {
            if(response.Content != null)
            {
                await response.Content.LoadIntoBufferAsync();
                var messageContent = new HttpMessageContent(response);
                var buffer = await messageContent.ReadAsByteArrayAsync().ConfigureAwait(false);
                await stream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            }
            else
            {
                var httpMessageContent = new HttpMessageContent(response);
                var buffer = await httpMessageContent.ReadAsByteArrayAsync().ConfigureAwait(false);
                await stream.WriteAsync(buffer, 0, buffer.Length);
            }
        }

        public async Task Serialize(HttpRequestMessage request, Stream stream)
        {
            if(request.Content != null)
            {
                await request.Content.LoadIntoBufferAsync();
                var messageContent = new HttpMessageContent(request);
                var buffer = await messageContent.ReadAsByteArrayAsync();
                await stream.WriteAsync(buffer, 0, buffer.Length);
            }
            else
            {
                var httpMessageContent = new HttpMessageContent(request);
                var buffer = await httpMessageContent.ReadAsByteArrayAsync();
                await stream.WriteAsync(buffer, 0, buffer.Length);
            }
        }

        public Task<HttpResponseMessage> DeserializeToResponse(Stream stream)
        {
            var response = new HttpResponseMessage();
            response.Content = new StreamContent(stream);
            response.Content.Headers.Add("Content-Type", "application/http;msgtype=response");
            return response.Content.ReadAsHttpResponseMessageAsync();
        }

        public async Task<HttpRequestMessage> DeserializeToRequest(Stream stream)
        {
            var request = new HttpRequestMessage
            {
                Content = new StreamContent(stream)
            };
            request.Content.Headers.Add("Content-Type", "application/http;msgtype=request");
            return await request.Content.ReadAsHttpRequestMessageAsync();
        }
    }
}