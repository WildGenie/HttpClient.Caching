namespace HttpClient.Caching.CacheStore
{
    using System.IO;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using HttpClient.Caching.Cache;
    using Shouldly;
    using Xunit;

    public class RequestSerializationTests
    {
        [Fact]
        public async Task IntegrationTest_Serialize()
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "http://some.server/api/foo");
            requestMessage.Headers.Range = new RangeHeaderValue(0, 1) {Unit = "custom"};
            var memoryStream = new MemoryStream();
            await MessageContentSerializer.Serialize(requestMessage, memoryStream);
            memoryStream.Position = 0;

            var request = await MessageContentSerializer.DeserializeToRequest(memoryStream);

            requestMessage.Headers.Range.Unit.ShouldBe(request.Headers.Range.Unit);
        }
    }
}