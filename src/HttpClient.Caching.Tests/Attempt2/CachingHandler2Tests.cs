namespace HttpClient.Caching.Attempt2
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using HttpClient.Caching.Headers;
    using Shouldly;
    using Xunit;

    public class CachingHandler2Tests
    {
        private readonly FakeServerMessageHandler _serverMessageHandler;
        private readonly HttpClient _client;
        private readonly InMemoryCacheStore2 _cacheStore;
        private readonly HttpResponseMessage _serverResponse;

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

            _serverResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<html></html>")
            };
            _serverResponse.Headers.CacheControl = new CacheControlHeaderValue
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(200)
            };
            _serverMessageHandler.Response = _serverResponse;
        }

        [Fact]
        public async Task Cacheable_response_should_be_cached()
        {
            await _client.GetAsync("");
            var response = await _client.GetAsync("");

            response.Headers.GetCachingHeader()
                .RetrievedFromCache?.ShouldBe(true);
        }

        [Fact]
        public async Task Non_cachable_status_404_should_npt_cache_response()
        {
            _serverResponse.StatusCode = HttpStatusCode.NotFound;
            await _client.GetAsync("");
            var response = await _client.GetAsync("");

            response.Headers.GetCachingHeader()
                .NotCacheable?.ShouldBe(false);
        }

        [Fact]
        public async Task When_no_store_in_request_then_should_not_cache()
        {
            await _client.GetAsync(""); // Warm the cache

            var noStoreRequest = new HttpRequestMessage(HttpMethod.Get, "");
            noStoreRequest.Headers.CacheControl = new CacheControlHeaderValue
            {
                NoStore = true
            };

            var response = await _client.SendAsync(noStoreRequest);

            var cachingHeader = response.Headers.GetCachingHeader();
            cachingHeader.RetrievedFromCache?.ShouldBe(false);
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