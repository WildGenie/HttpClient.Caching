namespace CacheCow.Client.Tests
{
    using System.Collections.Generic;
    using NUnit.Framework;

    public class InMemoryVaryHeaderStoreTests
    {
        private const string TestUrl = "/api/Test?a=1";

        [Test]
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
            Assert.IsTrue(result);
            Assert.IsNotNull(headers);
            Assert.AreEqual(hdrs, headers);
        }

        [Test]
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
            Assert.IsFalse(result);
            Assert.IsNull(headers);
            Assert.IsTrue(tryRemove);
        }

        [Test]
        public void Test_Get_NonExisting()
        {
            // arrange
            var store = new InMemoryVaryHeaderStore();
            IEnumerable<string> headers = null;

            // act
            var result = store.TryGetValue(TestUrl, out headers);

            // assert
            Assert.IsFalse(result);
            Assert.IsNull(headers);
        }
    }
}