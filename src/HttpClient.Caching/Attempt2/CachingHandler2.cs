namespace HttpClient.Caching.Attempt2
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public class CachingHandler2 : DelegatingHandler
    {
        private readonly CachingHandler2Settings _settings;
        private static readonly HashSet<HttpStatusCode> CacheableStatuses = new HashSet<HttpStatusCode>(new[] {
            HttpStatusCode.OK, HttpStatusCode.NonAuthoritativeInformation, HttpStatusCode.PartialContent,
            HttpStatusCode.MultipleChoices, HttpStatusCode.MovedPermanently, HttpStatusCode.Gone
        });
        private readonly ICacheStore2 _cacheStore;

        public CachingHandler2(CachingHandler2Settings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            _settings = settings;
            _cacheStore = settings.CacheStore;
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
            HttpResponseMessage response;

            if (request.Headers.CacheControl?.NoStore == true)
            {
                return await HandleNoStoreRequest(request, cancellationToken);
            }

            var cacheKey = request.CreateCacheKey();
            var cacheEntry = await _cacheStore.Get(cacheKey);
            if (cacheEntry == null)
            {
                response = await base.SendAsync(request, cancellationToken);
                var cachingHeader = new CachingHeader();
                if (IsResponseCacheable(response))
                {
                    await _cacheStore.AddOrUpdate(cacheKey, request, response);
                }
                else
                {
                    cachingHeader.NotCacheable = false;
                }
                return response;
            }
            else
            {
                response = await cacheEntry.GetCachedResponse();

                //cachingHeader.RetrievedFromCache = true;
            }

            /*if (!IsResponseCacheable(response))
            {
                // Remove from cache
            }
            else
            {
                // Add to cache
            }*/

            //response.Headers.Add(CachingHeader.Name, cachingHeader.ToString());
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
            return response.RequestMessage.Headers.CacheControl?.NoStore != true;
        }

        private async Task<HttpResponseMessage> HandleNoStoreRequest(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            var cachingHeader = new CachingHeader
            {
                RetrievedFromCache = false,
                NotCacheable = true
            };
            response.Headers.Add(CachingHeader.Name, cachingHeader.ToString());
            return response;
        }
    }

    internal static class HttpRequestMessageExtensions
    {
        internal static CacheKey2 CreateCacheKey(this HttpRequestMessage request)
        {
            return new CacheKey2(request.Method, request.RequestUri);
        }
    }
}