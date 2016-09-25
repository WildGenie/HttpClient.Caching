namespace Marvin.HttpCache
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Marvin.HttpCache.Store;

    public class HttpCacheHandler : DelegatingHandler
    {
        private readonly ICacheStore _cacheStore;
        private readonly bool _enableClearRelatedResourceRepresentationsAfterPatch;
        private readonly bool _enableClearRelatedResourceRepresentationsAfterPut;
        private readonly bool _enableConditionalPatch;
        private readonly bool _enableConditionalPut;

        private bool _forceRevalidationOfStaleResourceRepresentations;

        /// <summary>
        ///     Instantiates the HttpCacheHandler
        /// </summary>
        public HttpCacheHandler()
            : this(new ImmutableInMemoryCacheStore(), new HttpCacheHandlerSettings())
        {}

        /// <summary>
        ///     Instantiates the HttpCacheHandler
        /// </summary>
        /// <param name="cacheStore">An instance of an implementation of ICacheStore</param>
        public HttpCacheHandler(ICacheStore cacheStore)
            : this(cacheStore, new HttpCacheHandlerSettings())
        {}

        /// <summary>
        ///     Instantiates the HttpCacheHandler
        /// </summary>
        /// <param name="cacheHandlerSettings">An instance of an implementation of IHttpCacheHandlerSettings</param>
        public HttpCacheHandler(IHttpCacheHandlerSettings cacheHandlerSettings)
            : this(new ImmutableInMemoryCacheStore(), cacheHandlerSettings)
        {}


        /// <summary>
        ///     Instantiates the HttpCacheHandler
        /// </summary>
        /// <param name="cacheStore">An instance of an implementation of ICacheStore</param>
        /// <param name="cacheHandlerSettings">An instance of an implementation of IHttpCacheHandlerSettings</param>
        public HttpCacheHandler(ICacheStore cacheStore, IHttpCacheHandlerSettings cacheHandlerSettings)
        {
            if (cacheStore == null)
            {
                throw new ArgumentNullException(nameof(cacheStore));
            }

            if (cacheHandlerSettings == null)
            {
                throw new ArgumentNullException(nameof(cacheHandlerSettings));
            }

            _cacheStore = cacheStore;

            _forceRevalidationOfStaleResourceRepresentations =
                cacheHandlerSettings.ForceRevalidationOfStaleResourceRepresentations;
            _enableConditionalPatch = cacheHandlerSettings.EnableConditionalPatch;
            _enableConditionalPut = cacheHandlerSettings.EnableConditionalPut;
            _enableClearRelatedResourceRepresentationsAfterPatch =
                cacheHandlerSettings.EnableClearRelatedResourceRepresentationsAfterPatch;
            _enableClearRelatedResourceRepresentationsAfterPut =
                cacheHandlerSettings.EnableClearRelatedResourceRepresentationsAfterPut;
        }


        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if ((request.Method == HttpMethod.Put) || (request.Method.Method.ToLower() == "patch"))
                return HandleHttpPutOrPatch(request, cancellationToken);
            if (request.Method == HttpMethod.Get)
                return HandleHttpGet(request, cancellationToken);
            return base.SendAsync(request, cancellationToken);
        }

        private Task<HttpResponseMessage> HandleHttpPutOrPatch(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var primaryCacheKey = CacheKeyHelpers.CreatePrimaryCacheKey(request);
            var cacheKey = CacheKeyHelpers.CreateCacheKey(primaryCacheKey);

            // cached + conditional PUT or cached + conditional PATCH
            if ((_enableConditionalPut && (request.Method == HttpMethod.Put))
                ||
                (_enableConditionalPatch && (request.Method.Method.ToLower() == "patch")))
            {
                var addCachingHeaders = false;
                HttpResponseMessage responseFromCache = null;

                // available in cache?
                var responseFromCacheAsTask = _cacheStore.Get(cacheKey);
                if (responseFromCacheAsTask.Result != null)
                {
                    addCachingHeaders = true;
                    responseFromCache = responseFromCacheAsTask.Result.HttpResponse;
                }

                if (addCachingHeaders)
                {
                    // set etag / lastmodified.  Both are set for better compatibility
                    // with different backend caching systems.  
                    if (responseFromCache.Headers.ETag != null)
                        request.Headers.Add(HttpHeaderConstants.IfMatch,
                            responseFromCache.Headers.ETag.ToString());

                    if (responseFromCache.Content.Headers.LastModified != null)
                        request.Headers.Add(HttpHeaderConstants.IfUnmodifiedSince,
                            responseFromCache.Content.Headers.LastModified.Value.ToString("r"));
                }
            }

            return HandleSendAndContinuationForPutPatch(cacheKey, request, cancellationToken);
        }


        private async Task<HttpResponseMessage> HandleHttpGet(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // get VaryByHeaders - order in the request shouldn't matter, so order them so the
            // rest of the logic doesn't result in different keys.
            var primaryCacheKey = CacheKeyHelpers.CreatePrimaryCacheKey(request); // request.RequestUri.ToString();

            // first, before even looking at the cache:
            // The Cache-Control: no-cache HTTP/1.1 header field is also intended for use in requests made by the client. 
            // It is a means for the browser to tell the server and any intermediate caches that it wants a 
            // fresh version of the resource. 

            if ((request.Headers.CacheControl != null) && request.Headers.CacheControl.NoCache)
            {
                return await HandleSend(
                    CacheKeyHelpers.CreateCacheKey(primaryCacheKey), request, cancellationToken, false);
            }

            var responseIsCached = false;
            HttpResponseMessage responseFromCache = null;
            // available in cache?
            var cacheEntriesFromCacheAsTask = await _cacheStore.Get(primaryCacheKey).ConfigureAwait(false);
            if (cacheEntriesFromCacheAsTask != default(IEnumerable<CacheEntry>))
            {
                var cacheEntriesFromCache = cacheEntriesFromCacheAsTask;

                // TODO: for all of these, check the varyby headers (secondary key).  
                // An item is a match if secondary & primary keys both match!
                responseFromCache = cacheEntriesFromCache.First().HttpResponse;
                responseIsCached = true;
            }

            if (responseIsCached)
            {
                // set the accompanying request message
                responseFromCache.RequestMessage = request;

                // Check conditions that might require us to revalidate/check

                // we must assume "the worst": get from server.

                var mustRevalidate = HttpResponseHelpers.MustRevalidate(responseFromCache);

                if (mustRevalidate)
                {
                    // we must revalidate - add headers to the request for validation.  
                    //  
                    // we add both ETag & IfModifiedSince for better interop with various
                    // server-side caching handlers. 
                    //
                    if (responseFromCache.Headers.ETag != null)
                        request.Headers.Add(HttpHeaderConstants.IfNoneMatch,
                            responseFromCache.Headers.ETag.ToString());

                    if (responseFromCache.Content.Headers.LastModified != null)
                        request.Headers.Add(HttpHeaderConstants.IfModifiedSince,
                            responseFromCache.Content.Headers.LastModified.Value.ToString("r"));

                    return await HandleSend(
                        CacheKeyHelpers.CreateCacheKey(primaryCacheKey), request, cancellationToken, true);
                }
                // response is allowed to be cached and there's
                // no need to revalidate: return the cached response
                return responseFromCache;
            }
            // response isn't cached.  Get it, and (possibly) add it to cache.
            return await HandleSend(CacheKeyHelpers.CreateCacheKey(primaryCacheKey), request,
                cancellationToken, false);
        }


        private async Task<HttpResponseMessage> HandleSend(CacheKey cacheKey, HttpRequestMessage request,
            CancellationToken cancellationToken, bool mustRevalidate)
        {
            var serverResponse = await base.SendAsync(request, cancellationToken);

            // if we had to revalidate & got a 304 returned, that means
            // we can get the response message from cache.
            if (mustRevalidate && (serverResponse.StatusCode == HttpStatusCode.NotModified))
            {
                var cacheEntry = _cacheStore.Get(cacheKey).Result;
                var responseFromCacheEntry = cacheEntry.HttpResponse;
                responseFromCacheEntry.RequestMessage = request;

                return responseFromCacheEntry;
            }

            if (serverResponse.IsSuccessStatusCode)
            {
                // ensure no NULL dates
                if (serverResponse.Headers.Date == null)
                    serverResponse.Headers.Date = DateTimeOffset.UtcNow;

                // check the response: is this response allowed to be cached?
                var isCacheable = HttpResponseHelpers.CanBeCached(serverResponse);

                if (isCacheable)
                {
                    await _cacheStore.Set(cacheKey, new CacheEntry(serverResponse));
                }

                // what about vary by headers (=> key should take this into account)?
            }

            return serverResponse;
        }


        private async Task<HttpResponseMessage> HandleSendAndContinuationForPutPatch(CacheKey cacheKey,
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var serverResponse = await base.SendAsync(request, cancellationToken);
            if (serverResponse.IsSuccessStatusCode)
            {
                // ensure no NULL dates
                if (serverResponse.Headers.Date == null)
                    serverResponse.Headers.Date = DateTimeOffset.UtcNow;

                // should we clear?

                if ((_enableClearRelatedResourceRepresentationsAfterPut &&
                     (request.Method == HttpMethod.Put))
                    ||
                    (_enableClearRelatedResourceRepresentationsAfterPatch &&
                     request.Method.Method.Equals("patch", StringComparison.OrdinalIgnoreCase)))
                {
                    // clear related resources 
                    // 
                    // - remove resource with cachekey.  This must be done, as there's no 
                    // guarantee the new response is cacheable.
                    //
                    // - look for resources in cache that start with 
                    // the cachekey + "?" for querystring.

                    await _cacheStore.Remove(cacheKey);
                    await _cacheStore.RemoveRange(cacheKey.PrimaryKey + "?");
                }


                // check the response: is this response allowed to be cached?
                var isCacheable = HttpResponseHelpers.CanBeCached(serverResponse);

                if (isCacheable)
                {
                    await _cacheStore.Set(cacheKey, new CacheEntry(serverResponse));
                }

                // what about vary by headers (=> key should take this into account)?
            }

            return serverResponse;
        }
    }
}