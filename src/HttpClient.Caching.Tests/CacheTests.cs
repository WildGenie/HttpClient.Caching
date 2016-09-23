namespace HttpClient.Caching
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Owin.Builder;
    using Owin;
    using Xunit;

    public class CacheTests
    {
        //[Fact]
        public async Task Blah()
        {
            var app = new AppBuilder();
            app.Run(ctx =>
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.ReasonPhrase = "Not Found";
                return Task.CompletedTask;
            });
            var appFunc = app.Build();
            var owinHandler = new OwinHttpMessageHandler(appFunc)
            {
                AllowAutoRedirect = true,
                UseCookies = true
            };

            var cachingHandler = new CachingHandler(owinHandler);

            var client = new HttpClient(cachingHandler)
            {
                BaseAddress = new Uri("http://example.com")
            };

            await client.GetAsync("");
        }
    }
}