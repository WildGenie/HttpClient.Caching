namespace Marvin.HttpCache.Tests
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Marvin.HttpCache.Store;
    using Shouldly;
    using Xunit;

    public class ImmutableInMemoryCacheStoreTests
    {
        [Fact]
        public async Task SetAndGetTestCacheKeyEquality()
        {
            var store = new ImmutableInMemoryCacheStore();

            var resp = new HttpResponseMessage();

            var cacheKey = new CacheKey("key", null);
            await store.SetAsync(cacheKey, new CacheEntry(resp));

            var fromCache = await store.GetAsync(new CacheKey("key", null));

            fromCache.HttpResponse.ShouldBe(resp);
        }


        [Fact]
        public async Task SetAndGetExisting()
        {
            var store = new ImmutableInMemoryCacheStore();

            var resp = new HttpResponseMessage();
            var cacheKey = new CacheKey("key", null);
            await store.SetAsync(cacheKey, new CacheEntry(resp));

            var respNew = new HttpResponseMessage();

            // overwrite
            await store.SetAsync(cacheKey, new CacheEntry(respNew));

            var fromCache = await store.GetAsync(cacheKey);

            fromCache.HttpResponse.ShouldBe(respNew);
        }


        [Fact]
        public async Task SetAndGetMultiple()
        {
            var store = new ImmutableInMemoryCacheStore();

            var resp = new HttpResponseMessage();
            var resp2 = new HttpResponseMessage();

            var cacheKey = new CacheKey("key", null);
            var cacheKey2 = new CacheKey("key2", null);

            await store.SetAsync(cacheKey, new CacheEntry(resp));
            await store.SetAsync(cacheKey2, new CacheEntry(resp2));

            var fromCache = await store.GetAsync(cacheKey);
            var fromCache2 = await store.GetAsync(cacheKey2);

            fromCache.HttpResponse.ShouldBe(resp);
            fromCache2.HttpResponse.ShouldBe(resp2);
        }

        [Fact]
        public async Task GetNonExisting()
        {
            var store = new ImmutableInMemoryCacheStore();

            var resp = new HttpResponseMessage();
            var cacheKey = new CacheKey("key", null);

            await store.SetAsync(cacheKey, new CacheEntry(resp));

            var fromCache = await store.GetAsync("key2");

            fromCache.ShouldBeNull();
        }

        [Fact]
        public async Task GetNonExistingFromEmpty()
        {
            var store = new ImmutableInMemoryCacheStore();
            var cacheKey = new CacheKey("key", null);
            var fromCache = await store.GetAsync(cacheKey);

            fromCache.ShouldBe(default(CacheEntry));
        }


        [Fact]
        public async Task SetAndClear()
        {
            var store = new ImmutableInMemoryCacheStore();

            var resp = new HttpResponseMessage();
            var cacheKey = new CacheKey("key", null);
            await store.SetAsync(cacheKey, new CacheEntry(resp));

            await store.ClearAsync();

            var fromCache = await store.GetAsync(cacheKey);

            fromCache.ShouldBe(default(CacheEntry));
        }

        [Fact]
        public async Task SetAndGetNew()
        {
            var store = new ImmutableInMemoryCacheStore();

            var resp = new HttpResponseMessage();

            var cacheKey = new CacheKey("key", null);
            await store.SetAsync(cacheKey, new CacheEntry(resp));

            var fromCache = await store.GetAsync(cacheKey);

            fromCache.HttpResponse.ShouldBe(resp);
        }
    }
}