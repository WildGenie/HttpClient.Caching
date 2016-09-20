namespace CacheCow.Client
{
    using System.IO;
    using System.Net.Http;

    public interface IHttpMessageSerializer
    {
        void Serialize(HttpResponseMessage response, Stream stream);
        void Serialize(HttpRequestMessage request, Stream stream);
        HttpResponseMessage DeserializeToResponse(Stream stream);
        HttpRequestMessage DeserializeToRequest(Stream stream);
    }
}