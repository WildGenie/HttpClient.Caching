namespace HttpClient.Caching.CacheStore
{
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using HttpClient.Caching.Helper;
    using Shouldly;
    using Xunit;

    public class SerialisationTests
    {
        [Fact]
        public async Task Request_Deserialize_Serialize()
        {
            var stream =
                Assembly.GetExecutingAssembly().GetManifestResourceStream("HttpClient.Caching.Data.Request.dat");
            var serializer = new MessageContentHttpMessageSerializer();
            var request = await serializer.DeserializeToRequest(stream);

            var memoryStream = new MemoryStream();
            serializer.Serialize(request, memoryStream).Wait();

            memoryStream.Position = 0;
            var request2 = await serializer.DeserializeToRequest(memoryStream);
            var result = DeepComparer.Compare(request, request2);

            result.Count().ShouldBe(0, string.Join("\r\n", result));
        }

        [Fact]
        public async Task Request_Deserialize_Serialize_File()
        {
            var stream =
                Assembly.GetExecutingAssembly().GetManifestResourceStream("HttpClient.Caching.Data.Request.dat");
            var serializer = new MessageContentHttpMessageSerializer();
            var request = await serializer.DeserializeToRequest(stream);

            using(var fileStream = new FileStream("request.tmp", FileMode.Create))
            {
                await serializer.Serialize(request, fileStream);

                fileStream.Position = 0;
                var request2 = await serializer.DeserializeToRequest(fileStream);
                var result = DeepComparer.Compare(request, request2);

                result.Count().ShouldBe(0, string.Join("\r\n", result));
            }
        }

        [Fact]
        public async Task Response_Deserialize_Serialize()
        {
            var stream =
                Assembly.GetExecutingAssembly().GetManifestResourceStream("HttpClient.Caching.Data.Response.dat");
            var serializer = new MessageContentHttpMessageSerializer();
            var response = await serializer.DeserializeToResponse(stream);

            var memoryStream = new MemoryStream();
            await serializer.Serialize(response, memoryStream);

            memoryStream.Position = 0;
            var response2 = await serializer.DeserializeToResponse(memoryStream);
            var result = DeepComparer.Compare(response, response2);

            result.Count().ShouldBe(0, string.Join("\r\n", result));
        }

        [Fact]
        public async Task Response_Deserialize_Serialize_File()
        {
            var stream =
                Assembly.GetExecutingAssembly().GetManifestResourceStream("HttpClient.Caching.Data.Response.dat");
            var serializer = new MessageContentHttpMessageSerializer();
            var response = await serializer.DeserializeToResponse(stream);

            using(var fileStream = new FileStream("response.tmp", FileMode.Create))
            {
                await serializer.Serialize(response, fileStream);

                fileStream.Position = 0;
                var response2 = await serializer.DeserializeToResponse(fileStream);
                var result = DeepComparer.Compare(response, response2);

                result.Count().ShouldBe(0, string.Join("\r\n", result));
            }
        }
    }
}