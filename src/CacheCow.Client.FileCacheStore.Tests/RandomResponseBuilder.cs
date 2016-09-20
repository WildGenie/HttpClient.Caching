namespace CacheCow.Client.FileCacheStore.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using CacheCow.Common.Helpers;

    internal class RandomResponseBuilder : HttpMessageHandler
    {
        private readonly Random _random = new Random();
        private readonly Dictionary<int, byte[]> _responses = new Dictionary<int, byte[]>();
        private readonly SHA1CryptoServiceProvider _sha1 = new SHA1CryptoServiceProvider();
        private readonly int _totalResponses;

        public RandomResponseBuilder() : this(100)
        {}

        public RandomResponseBuilder(int totalResponses)
        {
            _totalResponses = totalResponses;
            for(var i = 0; i < totalResponses; i++)
                _responses.Add(i, BuildRandomResponseContent());
        }

        private byte[] BuildRandomResponseContent()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            var bytes = new byte[_random.Next(100000)];
            _random.NextBytes(bytes);
            return bytes;
        }


        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Send(request).ToTask();
        }

        public HttpResponseMessage Send(HttpRequestMessage request)
        {
            var hash = _sha1.ComputeHash(Encoding.UTF8.GetBytes(request.RequestUri.ToString()));
            var i = Math.Abs(BitConverter.ToInt32(hash, 0));
            var bytes = _responses[i%_totalResponses];
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new ByteArrayContent(bytes)
            };
            response.Content.Headers.Add("content-type", "application/octet-stream");
            return response;
        }
    }
}