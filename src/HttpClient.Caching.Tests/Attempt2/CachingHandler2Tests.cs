namespace HttpClient.Caching.Attempt2
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class CachingHandler2Tests
    {
        private readonly FakeServerMessageHandler _serverMessageHandler;
        private readonly HttpClient _client;
        private readonly InMemoryCacheStore2 _cacheStore;

        public CachingHandler2Tests()
        {
            _serverMessageHandler = new FakeServerMessageHandler();
            _cacheStore = new InMemoryCacheStore2();
            var settings = new CachingHandler2Settings(_cacheStore);
            var cachingHandler = new CachingHandler2(settings)
            {
                InnerHandler = _serverMessageHandler
            };
            _client = new HttpClient(cachingHandler)
            {
                BaseAddress = new Uri("http://example.com")
            };
        }

        [Fact]
        public async Task Blah()
        {
            _serverMessageHandler.Response = new HttpResponseMessage(HttpStatusCode.OK);
            var response = await _client.GetAsync("index.html");
        }
    }

    internal class FakeServerMessageHandler : HttpMessageHandler
    {
        public HttpResponseMessage Response { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken _)
        {
            Response.RequestMessage = request;
            return Task.FromResult(Response);
        }
    }
}