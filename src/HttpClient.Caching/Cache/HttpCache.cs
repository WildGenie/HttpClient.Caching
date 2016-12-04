namespace HttpClient.Caching.Cache
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using HttpClient.Caching;
    using HttpClient.Caching.Infrastructure;

    public class HttpCache : IDisposable
    {
        private readonly IContentStore _contentStore;
        private readonly string _cachePath;
        private readonly bool _isShared;
        private readonly GetUtcNow _getUtcNow;
        private readonly TaskQueue _taskQueue = new TaskQueue();

        public readonly Func<HttpResponseMessage, bool> StoreBasedOnHeuristics = r => false;
        public readonly Dictionary<HttpMethod, object> CacheableMethods = new Dictionary<HttpMethod, object>
        {
            {HttpMethod.Get, null},
            {HttpMethod.Head, null},
            {HttpMethod.Post, null}
        };

        public HttpCache(IContentStore contentStore,
            string cachePath = "cache",
            bool isShared = false,
            GetUtcNow getUtcNow = null)
        {
            _contentStore = contentStore;
            _cachePath = cachePath;
            _isShared = isShared;
            _getUtcNow = getUtcNow ?? (() => DateTime.UtcNow);

            if (!Directory.Exists(cachePath))
            {
                Directory.CreateDirectory(cachePath);

                // TODO lock file to prevent directory being deleted or a second instance leveraging.
            }

            // TODO Delete all .tmp files? They're dead Jim.
        }

        public async Task<CacheQueryResult> QueryCacheAsync(HttpRequestMessage request)
        {
            // Do we have anything stored for this method and URI?  Return entries for all variants
            var cacheEntryList = await _contentStore.GetEntries(new CacheKey(request.RequestUri, request.Method)).ConfigureAwait(false);
            if (cacheEntryList == null)  // Should I use null or Count() == 0 ?
            {
                return CacheQueryResult.CannotUseCache();
            }

            // Find the first matching variant based on the vary header fields defined in the cacheEntry 
            // and the values in the request
            var selectedEntry = MatchVariant(request, cacheEntryList);

            // Do we have a matching variant representation?
            if (selectedEntry == null)
            {
                return CacheQueryResult.CannotUseCache();
            }

            // Get the complete response, including body based on the selected variant
            var response = await _contentStore.GetResponse(selectedEntry.VariantId).ConfigureAwait(false);

            // Do caching directives require that we revalidate it regardless of freshness?
            var cacheControlHeaderValue = request.Headers.CacheControl ?? new CacheControlHeaderValue();
            var requestCacheControl = cacheControlHeaderValue;
            if ((requestCacheControl.NoCache || selectedEntry.CacheControl.NoCache))
            {
                return CacheQueryResult.Revalidate(this, selectedEntry, response);
            }

            // Is it fresh?
            if (selectedEntry.IsFresh())
            {
                
                if (requestCacheControl.MinFresh != null)
                {
                    var age = CalculateAge(response);
                    if (age <= requestCacheControl.MinFresh)
                    {
                        return CacheQueryResult.ReturnStored(this, selectedEntry,response);
                    }
                }
                else
                {
                    return CacheQueryResult.ReturnStored(this, selectedEntry,response);    
                }
            }

            // Did the client say we can serve it stale?
            if (requestCacheControl.MaxStale)
            {
                if (requestCacheControl.MaxStaleLimit != null)
                {
                    if (_getUtcNow() - selectedEntry.Expires <= requestCacheControl.MaxStaleLimit)
                    {
                        return CacheQueryResult.ReturnStored(this, selectedEntry,response);
                    }
                }
                else
                {
                    return CacheQueryResult.ReturnStored(this, selectedEntry,response);    
                }
            }

            // Do we have a selector to allow us to do a conditional request to revalidate it?
            return selectedEntry.HasValidator 
                ? CacheQueryResult.Revalidate(this, selectedEntry, response) 
                : CacheQueryResult.CannotUseCache(); // Can't do anything to help
        }

        public bool CanStore(HttpResponseMessage response)
        {
            // Only cache responses from methods that allow their responses to be cached
            if(!CacheableMethods.ContainsKey(response.RequestMessage.Method))
            {
                return false;
            }
            
            // Ensure that storing is not explicitly prohibited
            if(response.RequestMessage.Headers.CacheControl != null &&
               response.RequestMessage.Headers.CacheControl.NoStore)
            {
                return false;
            }

            var cacheControlHeaderValue = response.Headers.CacheControl;
            if(cacheControlHeaderValue != null && cacheControlHeaderValue.NoStore)
            {
                return false;
            }

            if (_isShared)
            {
                if(cacheControlHeaderValue != null && cacheControlHeaderValue.Private)
                {
                    return false;
                }
                if (response.RequestMessage.Headers.Authorization != null )
                {
                    if (cacheControlHeaderValue == null || !(cacheControlHeaderValue.MustRevalidate
                                                    || cacheControlHeaderValue.SharedMaxAge != null
                                                    || cacheControlHeaderValue.Public))
                    {
                        return false;
                    }
                }

            }

            if(response.Content?.Headers.Expires != null)
            {
                return true;
            }
            if (cacheControlHeaderValue != null)
            {
                if(cacheControlHeaderValue.MaxAge != null)
                {
                    return true;
                }
                if(cacheControlHeaderValue.SharedMaxAge != null)
                {
                    return true;
                }
            }

            var sc = (int) response.StatusCode;
            if ( sc == 200 || sc == 203 || sc == 204 || 
                 sc == 206 || sc == 300 || sc == 301 || 
                 sc == 404 || sc == 405 || sc == 410 || 
                 sc == 414 || sc == 501)
            {
                return StoreBasedOnHeuristics(response);
            }

            return false;
        }

        public async Task UpdateFreshnessAsync(CacheQueryResult result, HttpResponseMessage notModifiedResponse )
        {
            var selectedEntry = result.SelectedEntry;

            UpdateCacheEntry(notModifiedResponse, selectedEntry);

            await _contentStore.UpdateEntry(selectedEntry, result.SelectedResponse).ConfigureAwait(false);  //TODO
        }

        public async Task StoreResponseAsync(HttpResponseMessage response)
        {
            var primaryCacheKey = new CacheKey(response.RequestMessage.RequestUri, response.RequestMessage.Method);

            CacheEntry selectedEntry = null;

            IEnumerable<CacheEntry> cacheEntries = await _contentStore.GetEntries(primaryCacheKey).ConfigureAwait(false);
            if (cacheEntries != null)
            {
                selectedEntry = MatchVariant(response.RequestMessage, cacheEntries);
            }

            if (selectedEntry != null)
            {
                UpdateCacheEntry(response, selectedEntry);
                await _contentStore.UpdateEntry(selectedEntry, response).ConfigureAwait(false);
            }
            else
            {
                selectedEntry = new CacheEntry(primaryCacheKey, response, _getUtcNow);
                UpdateCacheEntry(response, selectedEntry);
                await _contentStore.AddEntry(selectedEntry, response).ConfigureAwait(false);
            }
        }

        public async Task<HttpContent> CacheContent(HttpResponseMessage response)
        {
            var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            stream = new FancyStream(stream, _cachePath,
                 fileName => { _taskQueue.Enqueue(() => HandleCompleted(response, fileName)); });
            return new StreamContent(stream);
        }

        private Task HandleCompleted(HttpResponseMessage response, string filename)
        {
            return StoreResponseAsync(response);
        }

        private static CacheEntry MatchVariant(HttpRequestMessage request, IEnumerable<CacheEntry> cacheEntryList)
        {
            var selectedEntry = cacheEntryList?
                .Where(ce => ce.Match(request))
                .OrderByDescending(ce => ce.Date)
                .FirstOrDefault();
            return selectedEntry;
        }

        private void UpdateCacheEntry(HttpResponseMessage updatedResponse, CacheEntry entry)
        {
            var newExpires = GetExpireDate(updatedResponse);

            if (newExpires > entry.Expires)
            {
                entry.Expires = newExpires;
            }
            entry.Etag = updatedResponse.Headers.ETag?.Tag;
            if (updatedResponse.Content != null)
            {
                entry.LastModified = updatedResponse.Content.Headers.LastModified;
            }
        }

        private DateTimeOffset GetExpireDate(HttpResponseMessage response)
        {
            if (response.Headers.CacheControl?.MaxAge != null)
            {
                return _getUtcNow() + response.Headers.CacheControl.MaxAge.Value;
            }
            return response.Content?.Headers.Expires ?? _getUtcNow();
        }

        public static void ApplyConditionalHeaders(CacheQueryResult result, HttpRequestMessage request)
        {
            Debug.Assert(result.SelectedEntry != null);
            if (result.SelectedEntry == null || !result.SelectedEntry.HasValidator) return;

            if (result.SelectedEntry.Etag != null)
            {
                request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(result.SelectedEntry.Etag));
            }
            else
            {
                if (result.SelectedEntry.LastModified != null)
                {
                    request.Headers.IfModifiedSince = result.SelectedEntry.LastModified;
                }
            }
        }

        public void UpdateAgeHeader(HttpResponseMessage response)
        {
            if (response.Headers.Date.HasValue)
            {
                response.Headers.Age = CalculateAge(response);
            }
        }

        public void Dispose()
        {
            _taskQueue.Dispose();
        }

        private TimeSpan CalculateAge(HttpResponseMessage response)
        {
            var age = _getUtcNow() - response.Headers.Date.Value;
            if(age.TotalMilliseconds < 0)
            {
                age = new TimeSpan(0);
            }
            return new TimeSpan(0, 0, (int) Math.Round(age.TotalSeconds));
        }

        private class FancyStream : Stream
        {
            private readonly string _fileName;
            private readonly FileStream _fileStream;
            private readonly Stream _inner;
            private readonly Action<string> _onComplete;
            private readonly string _tempFileName;

            public FancyStream(Stream inner, string cachePath, Action<string> onComplete)
            {
                _inner = inner;
                _onComplete = onComplete;
                _fileName = Path.Combine(cachePath, Guid.NewGuid().ToString());
                _tempFileName = $"{_fileName}.tmp";
                _fileStream = File.Open(_tempFileName, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            }

            public override bool CanRead => _inner.CanRead;

            public override bool CanSeek => _inner.CanSeek;

            public override bool CanWrite => _inner.CanWrite;

            public override long Length => _inner.Length;

            public override long Position
            {
                get { return _inner.Position; }
                set { _inner.Position = value; }
            }

            public override void Flush()
            {
                _inner.Flush();
                _fileStream.Flush();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _inner.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                _inner.SetLength(value);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                var read = _inner.Read(buffer, offset, count);
                _fileStream.Write(buffer, offset, read);

                if (read == 0)
                {
                    Complete();
                }
                else
                {
                    _fileStream.Write(buffer, offset, read);
                }

                return read;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _inner.Write(buffer, offset, count);
            }

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count,
                CancellationToken cancellationToken)
            {
                var read = await _inner.ReadAsync(buffer, offset, count, cancellationToken);

                if (read == 0)
                {
                    Complete();
                }
                else
                {
                    await _fileStream.WriteAsync(buffer, offset, read, cancellationToken);
                }

                return read;
            }

            public override void Close()
            {
                _inner.Close();
            }

            private void Complete()
            {
                _fileStream.Close();
                File.Move(_tempFileName, _fileName);
                _onComplete(_fileName);
            }
        }
    }
}