namespace HttpClient.Caching
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using Shouldly;
    using Xunit;

    public class ResponseValidatorTests
    {
        [Theory]
        [InlineData(HttpStatusCode.NotFound)]
        [InlineData(HttpStatusCode.NotModified)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public void Test_Not_Cacheable_StatusCode(HttpStatusCode code)
        {
            var cachingHandler = new CachingHandler();
            var response = new HttpResponseMessage(code);
            cachingHandler.ResponseValidator(response).ShouldBe(ResponseValidationResult.NotCacheable);
        }

        [Fact]
        public void Test_Must_Revalidate()
        {
            var cachingHandler = new CachingHandler();
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.CacheControl = new CacheControlHeaderValue
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(200),
                MustRevalidate = true
            };
            response.Headers.Date = DateTimeOffset.UtcNow;
            response.Content = new ByteArrayContent(new byte[256]);
            response.Content.Headers.Expires = DateTime.Now.Subtract(TimeSpan.FromSeconds(10));
            cachingHandler.ResponseValidator(response).ShouldBe(ResponseValidationResult.MustRevalidate);
        }

        [Fact]
        public void Test_NoCache_IsCacheable_And_NotStale_But_MustRevalidate()
        {
            var cachingHandler = new CachingHandler();
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.CacheControl = new CacheControlHeaderValue {Public = true, NoCache = true};
            response.Content = new ByteArrayContent(new byte[256]);
            response.Content.Headers.Expires = DateTimeOffset.Now.AddHours(1); // resource is not stale
            cachingHandler.ResponseValidator(response).ShouldBe(ResponseValidationResult.MustRevalidate);
        }

        [Fact]
        public void Test_Not_Cacheable_No_CacheControl()
        {
            var cachingHandler = new CachingHandler();
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            cachingHandler.ResponseValidator(response).ShouldBe(ResponseValidationResult.NotCacheable);
        }

        [Fact]
        public void Test_Not_Cacheable_No_Content()
        {
            var cachingHandler = new CachingHandler();
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.CacheControl = new CacheControlHeaderValue {Public = true};
            cachingHandler.ResponseValidator(response).ShouldBe(ResponseValidationResult.NotCacheable);
        }

        [Fact]
        public void Test_Not_Cacheable_No_Expiration()
        {
            var cachingHandler = new CachingHandler();
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.CacheControl = new CacheControlHeaderValue {Public = true};
            response.Content = new ByteArrayContent(new byte[256]);
            cachingHandler.ResponseValidator(response).ShouldBe(ResponseValidationResult.NotCacheable);
        }

        [Fact]
        public void Test_OK()
        {
            var cachingHandler = new CachingHandler();
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.CacheControl = new CacheControlHeaderValue
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(200),
                MustRevalidate = false
            };
            response.Headers.Date = DateTimeOffset.UtcNow;
            response.Content = new ByteArrayContent(new byte[256]);
            cachingHandler.ResponseValidator(response).ShouldBe(ResponseValidationResult.OK);
        }

        [Fact]
        public void Test_Stale_By_Age()
        {
            var cachingHandler = new CachingHandler();
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.CacheControl = new CacheControlHeaderValue
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(200)
            };
            response.Headers.Date = DateTimeOffset.UtcNow.AddDays(-1);
            response.Content = new ByteArrayContent(new byte[256]);
            cachingHandler.ResponseValidator(response).ShouldBe(ResponseValidationResult.MustRevalidate);
        }

        [Fact]
        public void Test_Stale_By_Age_MustRevalidateByDefaultOFF()
        {
            var cachingHandler = new CachingHandler();
            cachingHandler.MustRevalidateByDefault = false;

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.CacheControl = new CacheControlHeaderValue
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(200)
            };
            response.Headers.Date = DateTimeOffset.UtcNow.AddDays(-1);
            response.Content = new ByteArrayContent(new byte[256]);
            cachingHandler.ResponseValidator(response).ShouldBe(ResponseValidationResult.Stale);
        }


        [Fact]
        public void Test_Stale_By_Expires()
        {
            var cachingHandler = new CachingHandler();
            cachingHandler.MustRevalidateByDefault = false;

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.CacheControl = new CacheControlHeaderValue {Public = true};
            response.Content = new ByteArrayContent(new byte[256]);
            response.Content.Headers.Expires = DateTimeOffset.UtcNow.AddDays(-1);
            cachingHandler.ResponseValidator(response).ShouldBe(ResponseValidationResult.Stale);
        }

        [Fact]
        public void Test_Stale_By_SharedAge()
        {
            var cachingHandler = new CachingHandler();
            cachingHandler.MustRevalidateByDefault = false;

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.CacheControl = new CacheControlHeaderValue
            {
                Public = true,
                SharedMaxAge = TimeSpan.FromSeconds(200)
            };
            response.Headers.Date = DateTimeOffset.UtcNow.AddDays(-1);
            response.Content = new ByteArrayContent(new byte[256]);
            cachingHandler.ResponseValidator(response).ShouldBe(ResponseValidationResult.Stale);
        }
    }
}