namespace CacheCow.Client
{
    using System.Diagnostics;
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

        public Task SerializeAsync(Task<HttpResponseMessage> response, Stream stream)
        {
            return response.Then(r =>
                {
                    if(r.Content != null)
                    {
                        TraceWriter.WriteLine("SerializeAsync - before load",
                            TraceLevel.Verbose);

                        return r.Content.LoadIntoBufferAsync()
                            .Then(() =>
                            {
                                TraceWriter.WriteLine("SerializeAsync - after load", TraceLevel.Verbose);
                                var httpMessageContent2 = new HttpMessageContent(r);
                                // All in-memory and CPU-bound so no need to async
                                return httpMessageContent2.ReadAsByteArrayAsync();
                            })
                            .Then(buffer =>
                                {
                                    TraceWriter.WriteLine("SerializeAsync - after ReadAsByteArrayAsync",
                                        TraceLevel.Verbose);
                                    return Task.Factory.FromAsync(stream.BeginWrite, stream.EndWrite,
                                        buffer, 0, buffer.Length, null, TaskCreationOptions.AttachedToParent);
                                }
                            );

                        ;
                    }
                    TraceWriter.WriteLine("Content NULL - before load",
                        TraceLevel.Verbose);

                    var httpMessageContent = new HttpMessageContent(r);
                    // All in-memory and CPU-bound so no need to async
                    var buffer2 = httpMessageContent.ReadAsByteArrayAsync().Result;
                    return Task.Factory.FromAsync(stream.BeginWrite, stream.EndWrite,
                        buffer2, 0, buffer2.Length, null, TaskCreationOptions.AttachedToParent);
                }
            );
        }

        public Task SerializeAsync(HttpRequestMessage request, Stream stream)
        {
            if(request.Content != null)
                return request.Content.LoadIntoBufferAsync()
                    .Then(() =>
                    {
                        var httpMessageContent2 = new HttpMessageContent(request);
                        // All in-memory and CPU-bound so no need to async
                        httpMessageContent2.ReadAsByteArrayAsync().Then(
                            buffer =>
                            {
                                return Task.Factory.FromAsync(stream.BeginWrite, stream.EndWrite,
                                    buffer, 0, buffer.Length, null, TaskCreationOptions.AttachedToParent);
                            });
                    });
            var httpMessageContent = new HttpMessageContent(request);
            // All in-memory and CPU-bound so no need to async
            return httpMessageContent.ReadAsByteArrayAsync().Then(
                buffer =>
                {
                    return Task.Factory.FromAsync(stream.BeginWrite, stream.EndWrite,
                        buffer, 0, buffer.Length, null, TaskCreationOptions.AttachedToParent);
                }
            );
        }

        public Task<HttpResponseMessage> DeserializeToResponseAsync(Stream stream)
        {
            var response = new HttpResponseMessage();
            response.Content = new StreamContent(stream);
            response.Content.Headers.Add("Content-Type", "application/http;msgtype=response");
            TraceWriter.WriteLine("before ReadAsHttpResponseMessageAsync",
                TraceLevel.Verbose);
            return response.Content.ReadAsHttpResponseMessageAsync();
        }

        public Task<HttpRequestMessage> DeserializeToRequestAsync(Stream stream)
        {
            var request = new HttpRequestMessage();
            request.Content = new StreamContent(stream);
            request.Content.Headers.Add("Content-Type", "application/http;msgtype=request");
            return request.Content.ReadAsHttpRequestMessageAsync();
        }
    }
}