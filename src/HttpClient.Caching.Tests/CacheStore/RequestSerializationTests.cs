namespace HttpClient.Caching.CacheStore
{
    using System.IO;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Shouldly;
    using Xunit;

    public class RequestSerializationTests
    {
        [Fact]
        public async Task IntegrationTest_Serialize()
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "http://some.server/api/foo");
            requestMessage.Headers.Range = new RangeHeaderValue(0, 1) {Unit = "custom"};
            var serializer = new MessageContentHttpMessageSerializer();
            var memoryStream = new MemoryStream();
            await serializer.Serialize(requestMessage, memoryStream);
            memoryStream.Position = 0;

            var request = await serializer.DeserializeToRequest(memoryStream);

            requestMessage.Headers.Range.Unit.ShouldBe(request.Headers.Range.Unit);
        }
    }
}