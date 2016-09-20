namespace CacheCow.Common.Http
{
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;

    public class NotModifiedResponse : HttpResponseMessage
    {
        public NotModifiedResponse(HttpRequestMessage request)
            : this(request, null)
        {}


        public NotModifiedResponse(HttpRequestMessage request, EntityTagHeaderValue etag)
            : base(HttpStatusCode.NotModified)
        {
            if(etag != null)
                Headers.ETag = etag;

            RequestMessage = request;
        }
    }
}