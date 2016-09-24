namespace HttpClient.Caching
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    internal class DummyMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage Request { get; set; }

        public HttpResponseMessage Response { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(Response);
        }
    }
}