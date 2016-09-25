namespace Marvin.HttpCache.Tests.Mock
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpResponseMessage Response { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Response);
        }
    }
}