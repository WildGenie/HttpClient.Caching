namespace HttpClient.Caching
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using HttpClient.Caching.CacheStore;
    using Shouldly;
    using Xunit;

    public class CachingHandlerTests
    {
        private const string DummyUrl = "http://myserver/api/dummy";
        private const string ETagValue = "\"abcdef\"";
        private HttpClient _client;
        private ICacheStore _cacheStore;
        private readonly DummyMessageHandler _messageHandler;
        private CachingHandler _cachingHandler;

        public CachingHandlerTests()
        {
            _messageHandler = new DummyMessageHandler();
            _cachingHandler = new CachingHandler(_cacheStore)
            {
                InnerHandler = _messageHandler
            };
            _client = new HttpClient(_cachingHandler);
        }

        private HttpResponseMessage GetOkMessage(bool mustRevalidate = false)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.CacheControl = new CacheControlHeaderValue
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(200),
                MustRevalidate = mustRevalidate
            };
            response.Headers.Date = DateTimeOffset.UtcNow;
            response.Content = new ByteArrayContent(new byte[256]);
            return response;
        }

        [Fact]
        //[ExpectedException(typeof(NotImplementedException))]
        public async Task DefaultExceptionPolicy_Throws_CacheStore_Exceptions()
        {
            // setup 
            var request = new HttpRequestMessage(HttpMethod.Put, DummyUrl);
            var responseFromServer = GetOkMessage();
            _messageHandler.Response = responseFromServer;
            var cacheStore = new FaultyCacheStore();
            var cachingHandler = new CachingHandler(cacheStore)
            {
                InnerHandler = _messageHandler
            };
            _client = new HttpClient(cachingHandler);


            // run
            await Should.ThrowAsync<NotImplementedException>(_client.SendAsync(request));
        }

        [Fact]
        public void Get_Must_Revalidate_Etag_NotModified()
        {
            // setup 
            var request = new HttpRequestMessage(HttpMethod.Get, DummyUrl);
            var responseFromCache = GetOkMessage(true);
            responseFromCache.Headers.ETag = new EntityTagHeaderValue(ETagValue);
            responseFromCache.Content.Headers.Expires = DateTime.Now.Subtract(TimeSpan.FromSeconds(10));
            var responseFromServer = new HttpResponseMessage(HttpStatusCode.NotModified);
            _messageHandler.Response = responseFromServer;
            _cacheStore.Expect(
                    x => x.TryGetValue(Arg<CacheKey>.Is.Anything, out Arg<HttpResponseMessage>.Out(responseFromCache).Dummy))
                .Return(true);
            _cacheStore.Expect(x => x.AddOrUpdate(Arg<CacheKey>.Is.Anything, Arg<HttpResponseMessage>.Is.Anything));

            _mockRepository.ReplayAll();

            // run
            var task = _client.SendAsync(request);
            var responseReturned = task.Result;
            var header = responseReturned.Headers.Single(x => x.Key == CacheCowHeader.Name);
            CacheCowHeader cacheCowHeader = null;
            CacheCowHeader.TryParse(header.Value.First(), out cacheCowHeader);

            // verify
            _mockRepository.VerifyAll();
            Assert.IsNotNull(cacheCowHeader);
            Assert.AreEqual(ETagValue, request.Headers.IfNoneMatch.First().Tag);
            Assert.AreSame(responseFromCache, responseReturned);
            Assert.AreEqual(true, cacheCowHeader.CacheValidationApplied);
        }

        [Fact]
        public void Get_Must_Revalidate_Expires_Modified()
        {
            // setup 
            var request = new HttpRequestMessage(HttpMethod.Get, DummyUrl);
            var lastModified = DateTimeOffset.UtcNow.AddHours(-1);
            lastModified = lastModified.AddMilliseconds(1000 - lastModified.Millisecond);
            var responseFromCache = GetOkMessage(true);
            responseFromCache.Content.Headers.LastModified = lastModified;
            var responseFromServer = GetOkMessage();
            responseFromCache.Content.Headers.Expires = DateTime.Now.Subtract(TimeSpan.FromSeconds(10));

            _messageHandler.Response = responseFromServer;
            _cacheStore.Expect(x => x.TryGetValue(Arg<CacheKey>.Is.Anything,
                out Arg<HttpResponseMessage>.Out(responseFromCache).Dummy)).Return(true);
            _cacheStore.Expect(x => x.AddOrUpdate(Arg<CacheKey>.Is.Anything,
                Arg<HttpResponseMessage>.Is.Same(responseFromServer)));

            _mockRepository.ReplayAll();

            // run
            var task = _client.SendAsync(request);
            var responseReturned = task.Result;
            var header = responseReturned.Headers.Single(x => x.Key == CacheCowHeader.Name);
            CacheCowHeader cacheCowHeader = null;
            CacheCowHeader.TryParse(header.Value.First(), out cacheCowHeader);

            // verify
            _mockRepository.VerifyAll();
            Assert.IsNotNull(cacheCowHeader);
            Assert.AreSame(responseFromServer, responseReturned);
            Assert.AreEqual(true, cacheCowHeader.CacheValidationApplied);
        }

        [Fact]
        public void Get_Must_Revalidate_Expires_NotModified()
        {
            // setup 
            var request = new HttpRequestMessage(HttpMethod.Get, DummyUrl);
            var lastModified = DateTimeOffset.UtcNow.AddHours(-1);
            lastModified = lastModified.AddMilliseconds(1000 - lastModified.Millisecond);
            var responseFromCache = GetOkMessage(true);
            responseFromCache.Content.Headers.LastModified = lastModified;
            responseFromCache.Content.Headers.Expires = DateTime.Now.Subtract(TimeSpan.FromSeconds(10));

            var responseFromServer = new HttpResponseMessage(HttpStatusCode.NotModified);
            _messageHandler.Response = responseFromServer;
            _cacheStore.Expect(
                    x => x.TryGetValue(Arg<CacheKey>.Is.Anything, out Arg<HttpResponseMessage>.Out(responseFromCache).Dummy))
                .Return(true);
            _cacheStore.Expect(x => x.AddOrUpdate(Arg<CacheKey>.Is.Anything, Arg<HttpResponseMessage>.Is.Anything));

            _mockRepository.ReplayAll();

            // run
            var task = _client.SendAsync(request);
            var responseReturned = task.Result;
            var header = responseReturned.Headers.Single(x => x.Key == CacheCowHeader.Name);
            CacheCowHeader cacheCowHeader = null;
            CacheCowHeader.TryParse(header.Value.First(), out cacheCowHeader);

            // verify
            _mockRepository.VerifyAll();
            Assert.IsNotNull(cacheCowHeader);
            Assert.AreEqual(lastModified.ToString(), request.Headers.IfModifiedSince.Value.ToString());
            Assert.AreSame(responseFromCache, responseReturned);
            Assert.AreEqual(true, cacheCowHeader.CacheValidationApplied);
        }

        [Fact]
        public void Get_NoCache_Expires_ResultsInValidation()
        {
            // setup 
            var request = new HttpRequestMessage(HttpMethod.Get, DummyUrl);
            request.Headers.CacheControl = new CacheControlHeaderValue {NoCache = true};
            var lastModified = DateTimeOffset.UtcNow.AddHours(-1);
            lastModified = lastModified.AddMilliseconds(1000 - lastModified.Millisecond);
            var responseFromCache = GetOkMessage(false);
            responseFromCache.Content.Headers.LastModified = lastModified;
            var responseFromServer = GetOkMessage();
            responseFromCache.Content.Headers.Expires = DateTime.Now.Subtract(TimeSpan.FromSeconds(10));

            _messageHandler.Response = responseFromServer;
            _cacheStore.Expect(x => x.TryGetValue(Arg<CacheKey>.Is.Anything,
                out Arg<HttpResponseMessage>.Out(responseFromCache).Dummy)).Return(true);
            _cacheStore.Expect(x => x.AddOrUpdate(Arg<CacheKey>.Is.Anything,
                Arg<HttpResponseMessage>.Is.Same(responseFromServer)));

            _mockRepository.ReplayAll();

            // run
            var task = _client.SendAsync(request);
            var responseReturned = task.Result;
            var header = responseReturned.Headers.Single(x => x.Key == CacheCowHeader.Name);
            CacheCowHeader cacheCowHeader = null;
            CacheCowHeader.TryParse(header.Value.First(), out cacheCowHeader);

            // verify
            _mockRepository.VerifyAll();
            Assert.IsNotNull(cacheCowHeader);
            Assert.AreSame(responseFromServer, responseReturned);
            Assert.AreEqual(true, cacheCowHeader.CacheValidationApplied);
        }

        [Fact]
        public void Get_NoMustRevalidate_Expires_Modified()
        {
            // setup 
            var request = new HttpRequestMessage(HttpMethod.Get, DummyUrl);
            var lastModified = DateTimeOffset.UtcNow.AddHours(-1);
            lastModified = lastModified.AddMilliseconds(1000 - lastModified.Millisecond);
            var responseFromCache = GetOkMessage(false);
            responseFromCache.Content.Headers.LastModified = lastModified;
            var responseFromServer = GetOkMessage();
            responseFromCache.Content.Headers.Expires = DateTime.Now.Subtract(TimeSpan.FromSeconds(10));

            _messageHandler.Response = responseFromServer;
            _cacheStore.Expect(x => x.TryGetValue(Arg<CacheKey>.Is.Anything,
                out Arg<HttpResponseMessage>.Out(responseFromCache).Dummy)).Return(true);
            _cacheStore.Expect(x => x.AddOrUpdate(Arg<CacheKey>.Is.Anything,
                Arg<HttpResponseMessage>.Is.Same(responseFromServer)));

            _mockRepository.ReplayAll();

            // run
            var task = _client.SendAsync(request);
            var responseReturned = task.Result;
            var header = responseReturned.Headers.Single(x => x.Key == CacheCowHeader.Name);
            CacheCowHeader cacheCowHeader = null;
            CacheCowHeader.TryParse(header.Value.First(), out cacheCowHeader);

            // verify
            _mockRepository.VerifyAll();
            Assert.IsNotNull(cacheCowHeader);
            Assert.AreSame(responseFromServer, responseReturned);
            Assert.AreEqual(true, cacheCowHeader.CacheValidationApplied);
        }

        [Fact]
        public void Get_NoMustRevalidate_NoMustRevalidateByDefault_Expires_GetFromCache()
        {
            // setup 
            var request = new HttpRequestMessage(HttpMethod.Get, DummyUrl);
            var lastModified = DateTimeOffset.UtcNow.AddHours(-1);
            lastModified = lastModified.AddMilliseconds(1000 - lastModified.Millisecond);
            var responseFromCache = GetOkMessage(false); // NOTE !!
            _cachingHandler.MustRevalidateByDefault = false; // NOTE!!
            responseFromCache.Content.Headers.LastModified = lastModified;
            var responseFromServer = GetOkMessage();
            responseFromCache.Content.Headers.Expires = DateTime.Now.Subtract(TimeSpan.FromSeconds(10));

            _messageHandler.Response = responseFromServer;
            _cacheStore.Expect(x => x.TryGetValue(Arg<CacheKey>.Is.Anything,
                out Arg<HttpResponseMessage>.Out(responseFromCache).Dummy)).Return(true);

            _mockRepository.ReplayAll();

            // run
            var task = _client.SendAsync(request);
            var responseReturned = task.Result;
            var header = responseReturned.Headers.Single(x => x.Key == CacheCowHeader.Name);
            CacheCowHeader cacheCowHeader = null;
            CacheCowHeader.TryParse(header.Value.First(), out cacheCowHeader);

            // verify
            _mockRepository.VerifyAll();
            Assert.IsNotNull(cacheCowHeader);
            Assert.AreSame(responseFromCache, responseReturned);
            Assert.AreEqual(true, cacheCowHeader.WasStale);
        }

        [Fact]
        public void Get_NotModified_With_Stale_Client_Cache_Shall_Update_Date_Header()
        {
            // setup 
            var request = new HttpRequestMessage(HttpMethod.Get, DummyUrl);

            var responseFromCache = GetOkMessage(false);
            responseFromCache.Headers.Date = DateTimeOffset.UtcNow.AddHours(-1);
            responseFromCache.Headers.CacheControl.MaxAge = TimeSpan.FromSeconds(10);

            var responseFromServer = new HttpResponseMessage(HttpStatusCode.NotModified)
            {
                Content = new ByteArrayContent(new byte[256])
            };

            _messageHandler.Response = responseFromServer;
            _cacheStore.Expect(
                    x => x.TryGetValue(Arg<CacheKey>.Is.Anything, out Arg<HttpResponseMessage>.Out(responseFromCache).Dummy))
                .Return(true);
            _cacheStore.Expect(
                x =>
                    x.AddOrUpdate(Arg<CacheKey>.Is.Anything,
                        Arg<HttpResponseMessage>.Matches(
                            r => DateTimeOffset.UtcNow - r.Headers.Date.Value <= TimeSpan.FromSeconds(1))));

            _mockRepository.ReplayAll();


            // run
            var task = _client.SendAsync(request);
            var responseReturned = task.Result;
            var header = responseReturned.Headers.Single(x => x.Key == CacheCowHeader.Name);
            CacheCowHeader cacheCowHeader;
            CacheCowHeader.TryParse(header.Value.First(), out cacheCowHeader);


            // verify
            _mockRepository.VerifyAll();
            Assert.IsNotNull(cacheCowHeader);
            Assert.AreEqual(true, cacheCowHeader.CacheValidationApplied);
            Assert.AreEqual(true, cacheCowHeader.WasStale);
        }


        [Fact]
        public void Get_OK_But_Not_In_Cache_To_Insert_In_Cache()
        {
            // setup 
            var request = new HttpRequestMessage(HttpMethod.Get, DummyUrl);
            var response = GetOkMessage();
            _messageHandler.Response = response;
            _cacheStore.Expect(x => x.TryGetValue(Arg<CacheKey>.Is.Anything,
                out Arg<HttpResponseMessage>.Out(null).Dummy)).Return(false);
            _cacheStore.Expect(x => x.AddOrUpdate(Arg<CacheKey>.Is.Anything,
                Arg<HttpResponseMessage>.Is.Same(response)));

            _mockRepository.ReplayAll();

            // run
            var task = _client.SendAsync(request);
            var responseReturned = task.Result;
            var header = responseReturned.Headers.Single(x => x.Key == CacheCowHeader.Name);
            CacheCowHeader cacheCowHeader = null;
            CacheCowHeader.TryParse(header.Value.First(), out cacheCowHeader);
            // verify
            _mockRepository.VerifyAll();

            Assert.IsNotNull(cacheCowHeader);
            Assert.AreEqual(true, cacheCowHeader.DidNotExist);
        }

        [Fact]
        public void Get_Stale_And_In_Cache_To_Get_From_Cache()
        {
            // setup 
            var request = new HttpRequestMessage(HttpMethod.Get, DummyUrl);
            var response = GetOkMessage();
            _messageHandler.Response = response;
            _cacheStore.Expect(x => x.TryGetValue(Arg<CacheKey>.Is.Anything,
                out Arg<HttpResponseMessage>.Out(response).Dummy)).Return(true);

            _mockRepository.ReplayAll();

            // run
            var task = _client.SendAsync(request);
            var responseReturned = task.Result;
            var header = responseReturned.Headers.Single(x => x.Key == CacheCowHeader.Name);
            CacheCowHeader cacheCowHeader = null;
            CacheCowHeader.TryParse(header.Value.First(), out cacheCowHeader);
            // verify
            _mockRepository.VerifyAll();
            Assert.IsNotNull(cacheCowHeader);
            Assert.AreSame(response, responseReturned);
            Assert.AreEqual(true, cacheCowHeader.RetrievedFromCache);
        }


        [Fact]
        public void Get_Stale_ApplyValidation_NotModified()
        {
            // setup 
            var request = new HttpRequestMessage(HttpMethod.Get, DummyUrl);
            var responseFromCache = GetOkMessage();
            var then = DateTimeOffset.UtcNow.AddMilliseconds(-1);
            responseFromCache.Headers.Date = then;
            responseFromCache.Content.Headers.Expires = DateTimeOffset.Now.AddDays(-1);
            responseFromCache.Content.Headers.LastModified = DateTimeOffset.Now.AddDays(-2);
            var responseFromServer = GetOkMessage();
            responseFromServer.StatusCode = HttpStatusCode.NotModified;

            _messageHandler.Response = responseFromServer;
            _cacheStore.Expect(x => x.TryGetValue(Arg<CacheKey>.Is.Anything,
                out Arg<HttpResponseMessage>.Out(responseFromCache).Dummy)).Return(true);
            _cacheStore.Expect(
                x => x.AddOrUpdate(Arg<CacheKey>.Is.Anything, Arg<HttpResponseMessage>.Is.Equal(responseFromCache)));

            _mockRepository.ReplayAll();

            // run
            var task = _client.SendAsync(request);
            var responseReturned = task.Result;
            var header = responseReturned.Headers.Single(x => x.Key == CacheCowHeader.Name);
            CacheCowHeader cacheCowHeader = null;
            CacheCowHeader.TryParse(header.Value.First(), out cacheCowHeader);

            // verify
            _mockRepository.VerifyAll();
            Assert.IsNotNull(cacheCowHeader);
            Assert.AreSame(responseFromCache, responseReturned);
            Assert.AreEqual(true, cacheCowHeader.WasStale);
            Assert.AreEqual(true, cacheCowHeader.CacheValidationApplied);
            Assert.AreNotEqual(then, responseFromCache.Headers.Date);
        }

        [Fact]
        public void IgnoreExceptionPolicy_Ignores_CacheStore_Exceptions()
        {
            // setup 
            var request = new HttpRequestMessage(HttpMethod.Put, DummyUrl);
            var responseFromServer = GetOkMessage();
            _messageHandler.Response = responseFromServer;
            _cacheStore = new FaultyCacheStore();
            _cachingHandler = new CachingHandler(_cacheStore)
            {
                InnerHandler = _messageHandler
            };
            _cachingHandler.ExceptionHandler = CachingHandler.IgnoreExceptionPolicy;
            _client = new HttpClient(_cachingHandler);


            // run
            var task = _client.SendAsync(request);
            var responseReturned = task.Result;

            // verify
            Assert.AreEqual(responseFromServer, responseReturned);
        }

        [Fact]
        public void Methods_Other_Than_PUT_GET_Ignored()
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, DummyUrl);
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            _messageHandler.Response = httpResponseMessage;
            var task = _client.SendAsync(request);
            var response = task.Result;

            Assert.AreEqual(response, httpResponseMessage);
            Assert.IsNull(response.Headers.CacheControl);
            Assert.IsNull(request.Headers.CacheControl);
        }

        [Fact]
        public void NoStore_Ignored()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, DummyUrl);
            request.Headers.CacheControl = new CacheControlHeaderValue();
            request.Headers.CacheControl.NoStore = true;
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            _messageHandler.Response = httpResponseMessage;
            var task = _client.SendAsync(request);
            var response = task.Result;

            Assert.AreEqual(response, httpResponseMessage);
            Assert.IsNull(response.Headers.CacheControl);
        }

        [Fact]
        public void Put_Validate_Etag()
        {
            // setup 
            var request = new HttpRequestMessage(HttpMethod.Put, DummyUrl);
            var responseFromCache = GetOkMessage(true);
            responseFromCache.Headers.ETag = new EntityTagHeaderValue(ETagValue);
            var responseFromServer = new HttpResponseMessage(HttpStatusCode.NotModified);
            _messageHandler.Response = responseFromServer;
            _cacheStore.Expect(x => x.TryGetValue(Arg<CacheKey>.Is.Anything,
                out Arg<HttpResponseMessage>.Out(responseFromCache).Dummy)).Return(true);

            _mockRepository.ReplayAll();

            // run
            var task = _client.SendAsync(request);
            var responseReturned = task.Result;

            // verify
            _mockRepository.VerifyAll();
            Assert.AreEqual(ETagValue, request.Headers.IfMatch.First().Tag);
            Assert.AreSame(responseFromServer, responseReturned);
        }

        [Fact]
        public void Put_Validate_Expires()
        {
            // setup 
            var request = new HttpRequestMessage(HttpMethod.Put, DummyUrl);
            var lastModified = DateTimeOffset.UtcNow.AddHours(-1);
            lastModified = lastModified.AddMilliseconds(1000 - lastModified.Millisecond);
            var responseFromCache = GetOkMessage(true);
            responseFromCache.Content.Headers.LastModified = lastModified;
            var responseFromServer = GetOkMessage();
            _messageHandler.Response = responseFromServer;
            _cacheStore.Expect(x => x.TryGetValue(Arg<CacheKey>.Is.Anything,
                out Arg<HttpResponseMessage>.Out(responseFromCache).Dummy)).Return(true);

            _mockRepository.ReplayAll();

            // run
            var task = _client.SendAsync(request);
            var responseReturned = task.Result;

            // verify
            _mockRepository.VerifyAll();
            Assert.AreEqual(lastModified.ToString(), request.Headers.IfUnmodifiedSince.Value.ToString());
            Assert.AreSame(responseFromServer, responseReturned);
        }
    }

    public class FaultyCacheStore : ICacheStore
    {
        public void Dispose()
        {}

        public Task<bool> TryGetValue(CacheKey key, out HttpResponseMessage response)
        {
            throw new NotImplementedException();
        }

        public Task AddOrUpdate(CacheKey key, HttpResponseMessage response)
        {
            throw new NotImplementedException();
        }

        public Task<bool> TryRemove(CacheKey key)
        {
            throw new NotImplementedException();
        }

        public Task Clear()
        {
            throw new NotImplementedException();
        }
    }
}