namespace HttpClient.Caching.CacheStore
{
    using System.Collections.Generic;
    using Shouldly;
    using Xunit;

    public class InMemoryVaryHeaderStoreTests
    {
        private const string TestUrl = "/api/Test?a=1";

        [Fact]
        public void Test_Insert_Get()
        {
            // arrange
            var store = new InMemoryVaryHeaderStore();
            IEnumerable<string> headers = null;
            var hdrs = new[] {"a", "b"};

            // act
            store.AddOrUpdate(TestUrl, hdrs);
            var result = store.TryGetValue(TestUrl, out headers);

            // assert
            result.ShouldBeTrue();
            headers.ShouldNotBeNull();
            hdrs.ShouldBe(headers);
        }

        [Fact]
        public void Test_Insert_remove()
        {
            // arrange
            var store = new InMemoryVaryHeaderStore();
            IEnumerable<string> headers = null;
            var hdrs = new[] {"a", "b"};

            // act
            store.AddOrUpdate(TestUrl, hdrs);
            var tryRemove = store.TryRemove(TestUrl);
            var result = store.TryGetValue(TestUrl, out headers);

            // assert
            result.ShouldBeFalse();
            headers.ShouldBeNull();
            tryRemove.ShouldBeTrue();
        }

        [Fact]
        public void Test_Get_NonExisting()
        {
            // arrange
            var store = new InMemoryVaryHeaderStore();
            IEnumerable<string> headers = null;

            // act
            var result = store.TryGetValue(TestUrl, out headers);

            // assert
            result.ShouldBeFalse();
            headers.ShouldBeNull();
        }
    }
}