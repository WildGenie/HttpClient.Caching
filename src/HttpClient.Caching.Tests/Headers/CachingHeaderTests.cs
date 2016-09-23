namespace HttpClient.Caching.Headers
{
    using System;
    using System.Net.Http.Headers;
    using Shouldly;
    using Xunit;

    public class CachingHeaderTests
    {
        [Fact]
        public void DoesNotThrow_IfHeadersNull()
        {
            HttpResponseHeaders headers = null;
            headers.GetCachingHeader().ShouldBeNull();
        }

        [Fact]
        public void ParseTest_Successful()
        {
            CachingHeader header;
            var result = CachingHeader.TryParse("1.0;was-stale=true;not-cacheable=false;retrieved-from-cache=true;",
                out header);

            result.ShouldBeTrue();
            header.WasStale?.ShouldBeTrue();
            header.RetrievedFromCache?.ShouldBeTrue();
            header.NotCacheable?.ShouldBeFalse();
            header.DidNotExist.ShouldBeNull();
            header.CacheValidationApplied.ShouldBeNull();
        }

        [Fact]
        public void ToStringTest_Successful()
        {
            var cacheCowHeader = new CachingHeader
            {
                CacheValidationApplied = true,
                DidNotExist = false
            };

            var s = cacheCowHeader.ToString();

            s.IndexOf(CachingHeader.ExtensionNames.CacheValidationApplied + "=true", StringComparison.Ordinal)
                .ShouldBeGreaterThan(0);

            s.IndexOf(CachingHeader.ExtensionNames.DidNotExist + "=false", StringComparison.Ordinal)
                .ShouldBeGreaterThan(0);
        }
    }
}