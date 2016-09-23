namespace HttpClient.Caching
{
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using HttpClient.Caching.CacheStore;
    using HttpClient.Caching.Helper;
    using Shouldly;
    using Xunit;

    public class SerialisationTests
    {
        [Fact]
        public void Request_Deserialize_Serialize()
        {
            var stream =
                Assembly.GetExecutingAssembly().GetManifestResourceStream("HttpClient.Caching.Data.Request.cs");
            var serializer = new MessageContentHttpMessageSerializer();
            var request = serializer.DeserializeToRequestAsync(stream).Result;

            var memoryStream = new MemoryStream();
            serializer.SerializeAsync(request, memoryStream).Wait();

            memoryStream.Position = 0;
            var request2 = serializer.DeserializeToRequestAsync(memoryStream).Result;
            var result = DeepComparer.Compare(request, request2);

            result.Count().ShouldBe(0, string.Join("\r\n", result));
        }

        [Fact]
        public void Request_Deserialize_Serialize_File()
        {
            var stream =
                Assembly.GetExecutingAssembly().GetManifestResourceStream("HttpClient.Caching.Data.Request.cs");
            var serializer = new MessageContentHttpMessageSerializer();
            var request = serializer.DeserializeToRequestAsync(stream).Result;

            using(var fileStream = new FileStream("request.tmp", FileMode.Create))
            {
                serializer.SerializeAsync(request, fileStream).Wait();

                fileStream.Position = 0;
                var request2 = serializer.DeserializeToRequestAsync(fileStream).Result;
                var result = DeepComparer.Compare(request, request2);

                result.Count().ShouldBe(0, string.Join("\r\n", result));
            }
        }


        [Fact]
        public async Task Response_Deserialize_Serialize()
        {
            var stream =
                Assembly.GetExecutingAssembly().GetManifestResourceStream("HttpClient.Caching.Data.Response.cs");
            var serializer = new MessageContentHttpMessageSerializer();
            var response = await serializer.DeserializeToResponseAsync(stream);

            var memoryStream = new MemoryStream();
            await serializer.SerializeAsync(response, memoryStream);

            memoryStream.Position = 0;
            var response2 = await serializer.DeserializeToResponseAsync(memoryStream);
            var result = DeepComparer.Compare(response, response2);

            result.Count().ShouldBe(0, string.Join("\r\n", result));
        }

        [Fact]
        public async Task Response_Deserialize_Serialize_File()
        {
            var stream =
                Assembly.GetExecutingAssembly().GetManifestResourceStream("HttpClient.Caching.Data.Response.cs");
            var serializer = new MessageContentHttpMessageSerializer();
            var response = await serializer.DeserializeToResponseAsync(stream);

            using(var fileStream = new FileStream("response.tmp", FileMode.Create))
            {
                await serializer.SerializeAsync(response, fileStream);

                fileStream.Position = 0;
                var response2 = await serializer.DeserializeToResponseAsync(fileStream);
                var result = DeepComparer.Compare(response, response2);

                result.Count().ShouldBe(0, string.Join("\r\n", result));
            }
        }
    }
}