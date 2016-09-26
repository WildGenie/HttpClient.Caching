namespace HttpClient.Caching.Attempt2.CacheStore
{
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using HttpClient.Caching.Helper;
    using Shouldly;
    using Xunit;

    public class MessageContentSerialisationTests
    {
        [Fact]
        public async Task Request_Deserialize_Serialize()
        {
            var stream =
                Assembly.GetExecutingAssembly().GetManifestResourceStream("HttpClient.Caching.Data.Request.dat");
            var request = await MessageContentSerializer.DeserializeToRequest(stream);

            var memoryStream = new MemoryStream();
            await MessageContentSerializer.Serialize(request, memoryStream);

            memoryStream.Position = 0;
            var request2 = await MessageContentSerializer.DeserializeToRequest(memoryStream);
            var result = DeepComparer.Compare(request, request2);

            result.Count().ShouldBe(0, string.Join("\r\n", result));
        }

        [Fact]
        public async Task Request_Deserialize_Serialize_File()
        {
            var stream =
                Assembly.GetExecutingAssembly().GetManifestResourceStream("HttpClient.Caching.Data.Request.dat");
            var request = await MessageContentSerializer.DeserializeToRequest(stream);

            using(var fileStream = new FileStream("request.tmp", FileMode.Create))
            {
                await MessageContentSerializer.Serialize(request, fileStream);

                fileStream.Position = 0;
                var request2 = await MessageContentSerializer.DeserializeToRequest(fileStream);
                var result = DeepComparer.Compare(request, request2);

                result.Count().ShouldBe(0, string.Join("\r\n", result));
            }
        }

        [Fact]
        public async Task Response_Deserialize_Serialize()
        {
            var stream =
                Assembly.GetExecutingAssembly().GetManifestResourceStream("HttpClient.Caching.Data.Response.dat");
            var response = await MessageContentSerializer.DeserializeToResponse(stream);

            var memoryStream = new MemoryStream();
            await MessageContentSerializer.Serialize(response, memoryStream);

            memoryStream.Position = 0;
            var response2 = await MessageContentSerializer.DeserializeToResponse(memoryStream);
            var result = DeepComparer.Compare(response, response2);

            result.Count().ShouldBe(0, string.Join("\r\n", result));
        }

        [Fact]
        public async Task Response_Deserialize_Serialize_File()
        {
            var stream =
                Assembly.GetExecutingAssembly().GetManifestResourceStream("HttpClient.Caching.Data.Response.dat");
            var response = await MessageContentSerializer.DeserializeToResponse(stream);

            using(var fileStream = new FileStream("response.tmp", FileMode.Create))
            {
                await MessageContentSerializer.Serialize(response, fileStream);

                fileStream.Position = 0;
                var response2 = await MessageContentSerializer.DeserializeToResponse(fileStream);
                var result = DeepComparer.Compare(response, response2);

                result.Count().ShouldBe(0, string.Join("\r\n", result));
            }
        }
    }
}