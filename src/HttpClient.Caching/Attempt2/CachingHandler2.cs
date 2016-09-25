namespace HttpClient.Caching.Attempt2
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public delegate DateTime GetUtcNow();

    public class CachingHandler2 : DelegatingHandler
    {
        private readonly CachingHandler2Settings _settings;
        private static readonly HashSet<HttpStatusCode> CacheableStatuses = new HashSet<HttpStatusCode>(new[] {
            HttpStatusCode.OK, HttpStatusCode.NonAuthoritativeInformation, HttpStatusCode.PartialContent,
            HttpStatusCode.MultipleChoices, HttpStatusCode.MovedPermanently, HttpStatusCode.Gone
        });

        public CachingHandler2(CachingHandler2Settings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            _settings = settings;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get)
            {
                return HandleGetAsync(request, cancellationToken);
            }
            if (request.Method == HttpMethod.Head)
            {
                return HandleHeadAsync(request, cancellationToken);
            }
            return base.SendAsync(request, cancellationToken);
        }

        private async Task<HttpResponseMessage> HandleGetAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Check request is in cache?
            var response = await base.SendAsync(request, cancellationToken);
            if (!IsResponseCacheable(response))
            {
                // Remove from cache
            }
            else
            {
                // Add to cache
            }
            return response;
        }

        private async Task<HttpResponseMessage> HandleHeadAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            return response;
        }

        private bool IsResponseCacheable(HttpResponseMessage response)
        {
            if (!CacheableStatuses.Contains(response.StatusCode))
            {
                return false;
            }
            if ((response.RequestMessage.Headers.CacheControl?.NoStore == true)
                || (response.Headers.CacheControl?.NoStore == true))
            {
                return false;
            }
            return true;
        }
    }

    public class CachingHandler2Settings
    {
        private readonly ICacheStore2 _cacheStore2;
        private GetUtcNow _getUtcNow;

        public CachingHandler2Settings(ICacheStore2 cacheStore2 = null)
        {
            _cacheStore2 = cacheStore2 ?? new InMemoryCacheStore2();
        }

        public GetUtcNow GetUtcNow
        {
            get { return _getUtcNow ?? (() => DateTime.UtcNow); }
            set { _getUtcNow = value; }
        }
    }
}