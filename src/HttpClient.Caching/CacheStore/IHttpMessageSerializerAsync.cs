namespace HttpClient.Caching.CacheStore
{
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;

    public interface IHttpMessageSerializerAsync
    {
        Task Serialize(HttpResponseMessage response, Stream stream);

        Task Serialize(HttpRequestMessage request, Stream stream);

        Task<HttpResponseMessage> DeserializeToResponse(Stream stream);

        Task<HttpRequestMessage> DeserializeToRequest(Stream stream);
    }
}