namespace Marvin.HttpCache.Tests
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Marvin.HttpCache.Store;
    using Marvin.HttpCache.Tests.Mock;
    using Shouldly;
    using Xunit;

    public class HttpClientTests
    {
        private const string TestUri = "http://www.myapi.com/testresources";
        private const string ETag = "\"dummyetag\"";
        private ImmutableInMemoryCacheStore _store;
        private MockHttpMessageHandler _mockHandler;


        private HttpClient InitClient()
        {
            _store = new ImmutableInMemoryCacheStore();
            _mockHandler = new MockHttpMessageHandler();
            var httpClient = new HttpClient(
                new HttpCacheHandler(_store)
                {
                    InnerHandler = _mockHandler
                });

            return httpClient;
        }


        private HttpResponseMessage GetResponseMessage(bool mustRevalidate)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.Date = DateTimeOffset.UtcNow;
            response.Content = new ByteArrayContent(new byte[512]);
            response.Headers.CacheControl = new CacheControlHeaderValue
            {
                MustRevalidate = mustRevalidate,
                Public = true,
                MaxAge = TimeSpan.FromSeconds(666)
            };

            return response;
        }

        [Fact]
        public async Task GetFromCacheStoreNoRevalidate()
        {
            // first GET: insert in cache
            var req = new HttpRequestMessage(HttpMethod.Get, TestUri);
            var resp = GetResponseMessage(false);
            var httpClient = InitClient();

            _mockHandler.Response = resp;

            var result = await httpClient.SendAsync(req);

            // second GET: get from cache
            var req2 = new HttpRequestMessage(HttpMethod.Get, TestUri);
            var result2 = await httpClient.SendAsync(req2);

            // get from cache. Must match response, result and second result
            var fromCache = await _store.Get(new CacheKey(TestUri));

            result.ShouldBe(fromCache.HttpResponse);
            result.ShouldBe(resp);
            result.ShouldBe(result2);
        }

        [Fact]
        public async Task GetShouldInsertInCacheStore()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, TestUri);
            var resp = GetResponseMessage(false);
            var httpClient = InitClient();

            _mockHandler.Response = resp;

            var result = await httpClient.SendAsync(req);

            // result should be in cache
            var fromCache = await _store.Get(new CacheKey(TestUri));

            result.ShouldBe(fromCache.HttpResponse);
            result.ShouldBe(result);
        }

        [Fact]
        public async Task GetStaleFromCacheStoreNoRevalidate()
        {
            // first GET: insert in cache
            var req = new HttpRequestMessage(HttpMethod.Get, TestUri);
            var resp = GetResponseMessage(false);

            // ensure stale:
            resp.Headers.CacheControl.SharedMaxAge = new TimeSpan(-100);

            var httpClient = InitClient();

            _mockHandler.Response = resp;

            var result = await httpClient.SendAsync(req);

            // second GET: get from cache
            var req2 = new HttpRequestMessage(HttpMethod.Get, TestUri);
            var result2 = await httpClient.SendAsync(req2);

            // get from cache. Must match response, result and second result
            var fromCache = await _store.Get(new CacheKey(TestUri));

            result.ShouldBe(fromCache.HttpResponse);
            result.ShouldBe(resp);
            result.ShouldBe(result2);
        }


        [Fact]
        public async Task MustNotRevalidateEvenWithExpired()
        {
            // first GET: insert in cache
            var req = new HttpRequestMessage(HttpMethod.Get, TestUri);
            // no revalidation
            var resp = GetResponseMessage(false);

            resp.Headers.CacheControl.SharedMaxAge = null;
            resp.Headers.CacheControl.MaxAge = null;
            resp.Content.Headers.Expires = new DateTimeOffset(new DateTime(1, 1, 2));
            resp.Headers.ETag = new EntityTagHeaderValue(ETag);
            var httpClient = InitClient();

            _mockHandler.Response = resp;

            var result = await httpClient.SendAsync(req);

            // second GET: should not revalidate, just get from cache 
            var req2 = new HttpRequestMessage(HttpMethod.Get, TestUri);
            var result2 = await httpClient.SendAsync(req2);

            // get from cache. Must match response, result and second result
            var fromCache = await _store.Get(new CacheKey(TestUri));

            result.ShouldBe(fromCache.HttpResponse);
            result.ShouldBe(resp);
            result.ShouldBe(result2);
            req2.Headers.IfNoneMatch.FirstOrDefault().ShouldBeNull();
        }

        [Fact]
        public async Task MustNotRevalidateEvenWithSharedMaxAge()
        {
            // first GET: insert in cache
            var req = new HttpRequestMessage(HttpMethod.Get, TestUri);
            // no revalidation
            var resp = GetResponseMessage(false);

            resp.Headers.CacheControl.SharedMaxAge = new TimeSpan(-100);
            resp.Headers.ETag = new EntityTagHeaderValue(ETag);
            var httpClient = InitClient();

            _mockHandler.Response = resp;

            var result = await httpClient.SendAsync(req);

            // second GET: should not revalidate, just get from cache 
            var req2 = new HttpRequestMessage(HttpMethod.Get, TestUri);
            var result2 = await httpClient.SendAsync(req2);

            // get from cache. Must match response, result and second result
            var fromCache = await _store.Get(new CacheKey(TestUri));

            result.ShouldBe(fromCache.HttpResponse);
            result.ShouldBe(resp);
            result.ShouldBe(result2);
            req2.Headers.IfNoneMatch.FirstOrDefault().ShouldBeNull();
        }


        [Fact]
        public async Task MustNotRevalidateEvenWithToMaxAge()
        {
            // first GET: insert in cache
            var req = new HttpRequestMessage(HttpMethod.Get, TestUri);
            // no revalidation
            var resp = GetResponseMessage(false);

            resp.Headers.CacheControl.MaxAge = new TimeSpan(-100);
            resp.Headers.ETag = new EntityTagHeaderValue(ETag);
            var httpClient = InitClient();

            _mockHandler.Response = resp;

            var result = await httpClient.SendAsync(req);

            // second GET: should not revalidate, just get from cache 
            var req2 = new HttpRequestMessage(HttpMethod.Get, TestUri);
            var result2 = await httpClient.SendAsync(req2);

            // get from cache. Must match response, result and second result
            var fromCache = await _store.Get(new CacheKey(TestUri));

            result.ShouldBe(fromCache.HttpResponse);
            result.ShouldBe(resp);
            result.ShouldBe(result2);
            req2.Headers.IfNoneMatch.FirstOrDefault().ShouldBeNull();
        }


        [Fact]
        public async Task MustRevalidateDueToExpired()
        {
            // first GET: insert in cache
            var req = new HttpRequestMessage(HttpMethod.Get, TestUri);
            var resp = GetResponseMessage(true);

            resp.Headers.CacheControl.SharedMaxAge = null;
            resp.Headers.CacheControl.MaxAge = null;
            resp.Content.Headers.Expires = new DateTimeOffset(new DateTime(1, 1, 2));
            resp.Headers.ETag = new EntityTagHeaderValue(ETag);

            var httpClient = InitClient();

            _mockHandler.Response = resp;

            var result = await httpClient.SendAsync(req);

            // second GET: should revalidate and return 304 
            // which should then mean it returns the item from cache
            var req2 = new HttpRequestMessage(HttpMethod.Get, TestUri);
            var respNotModified = new HttpResponseMessage(HttpStatusCode.NotModified);
            _mockHandler.Response = respNotModified;

            var result2 = await httpClient.SendAsync(req2);

            // get from cache. Must match response, result and second result
            var fromCache = await _store.Get(new CacheKey(TestUri));

            result.ShouldBe(fromCache.HttpResponse);
            result.ShouldBe(resp);
            result.ShouldBe(result2);
            req2.Headers.IfNoneMatch.First().Tag.ShouldBe(ETag);
        }


        [Fact]
        public async Task MustRevalidateDueToMaxAge()
        {
            // first GET: insert in cache
            var req = new HttpRequestMessage(HttpMethod.Get, TestUri);
            var resp = GetResponseMessage(true);

            resp.Headers.CacheControl.MaxAge = new TimeSpan(-100);
            resp.Headers.ETag = new EntityTagHeaderValue(ETag);
            var httpClient = InitClient();

            _mockHandler.Response = resp;

            var result = await httpClient.SendAsync(req);

            // second GET: should revalidate and return 304 
            // which should then mean it returns the item from cache
            var req2 = new HttpRequestMessage(HttpMethod.Get, TestUri);
            var respNotModified = new HttpResponseMessage(HttpStatusCode.NotModified);
            _mockHandler.Response = respNotModified;

            var result2 = await httpClient.SendAsync(req2);

            // get from cache. Must match response, result and second result
            var fromCache = await _store.Get(new CacheKey(TestUri));

            result.ShouldBe(fromCache.HttpResponse);
            result.ShouldBe(resp);
            result.ShouldBe(result2);
            req2.Headers.IfNoneMatch.First().Tag.ShouldBe(ETag);
        }

        [Fact]
        public async Task MustRevalidateDueToNoCache()
        {
            // first GET: insert in cache
            var req = new HttpRequestMessage(HttpMethod.Get, TestUri);
            var resp = GetResponseMessage(false);

            // will revalidate even with "false" as mustrevalidate value
            resp.Headers.CacheControl.NoCache = true;
            resp.Headers.ETag = new EntityTagHeaderValue(ETag);
            var httpClient = InitClient();

            _mockHandler.Response = resp;

            var result = await httpClient.SendAsync(req);

            // second GET: should revalidate and return 304 
            // which should then mean it returns the item from cache
            var req2 = new HttpRequestMessage(HttpMethod.Get, TestUri);
            var respNotModified = new HttpResponseMessage(HttpStatusCode.NotModified);
            _mockHandler.Response = respNotModified;

            var result2 = await httpClient.SendAsync(req2);

            // get from cache. Must match response, result and second result
            var fromCache = await _store.Get(new CacheKey(TestUri));

            result.ShouldBe(fromCache.HttpResponse);
            result.ShouldBe(resp);
            result.ShouldBe(result2);
            req2.Headers.IfNoneMatch.First().Tag.ShouldBe(ETag);
        }

        [Fact]
        public async Task MustRevalidateDueToSharedMaxAge()
        {
            // first GET: insert in cache
            var req = new HttpRequestMessage(HttpMethod.Get, TestUri);
            var resp = GetResponseMessage(true);

            resp.Headers.CacheControl.SharedMaxAge = new TimeSpan(-100);
            resp.Headers.ETag = new EntityTagHeaderValue(ETag);
            var httpClient = InitClient();

            _mockHandler.Response = resp;

            var result = await httpClient.SendAsync(req);

            // second GET: should revalidate and return 304 
            // which should then mean it returns the item from cache
            var req2 = new HttpRequestMessage(HttpMethod.Get, TestUri);
            var respNotModified = new HttpResponseMessage(HttpStatusCode.NotModified);
            _mockHandler.Response = respNotModified;

            var result2 = await httpClient.SendAsync(req2);

            // get from cache. Must match response, result and second result
            var fromCache = await _store.Get(new CacheKey(TestUri));

            result.ShouldBe(fromCache.HttpResponse);
            result.ShouldBe(resp);
            result.ShouldBe(result2);
            req2.Headers.IfNoneMatch.First().Tag.ShouldBe(ETag);
        }

        [Fact]
        public async Task NoCacheDueToNoExpiresMaxAgeSharedMaxAge()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, TestUri);
            var resp = GetResponseMessage(false);
            var httpClient = InitClient();

            resp.Headers.CacheControl.MaxAge = null;
            resp.Headers.CacheControl.SharedMaxAge = null;
            resp.Content.Headers.Expires = null;
            _mockHandler.Response = resp;

            var result = await httpClient.SendAsync(req);

            // result should NOT be in cache
            var fromCache = await _store.Get(TestUri);
            fromCache.ShouldBeNull();
        }

        [Fact]
        public async Task NoCacheDueToNoStore()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, TestUri);
            var resp = GetResponseMessage(false);
            var httpClient = InitClient();

            // no store
            resp.Headers.CacheControl.NoStore = true;
            _mockHandler.Response = resp;

            var result = await httpClient.SendAsync(req);

            // result should NOT be in cache
            var fromCache = await _store.Get(TestUri);
            fromCache.ShouldBeNull();
        }
    }
}