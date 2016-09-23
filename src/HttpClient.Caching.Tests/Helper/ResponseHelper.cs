namespace HttpClient.Caching.Helper
{
    using System.Net.Http;
    using System.Reflection;
    using System.Text;
    using HttpClient.Caching.Headers;

    internal static class ResponseHelper
    {
        private const string HeadersResourcePath = "CacheCow.Client.Tests.Helper.Headers.txt";
        private static string _sampleHeaders;

        public static HttpResponseMessage GetMessage(HttpContent content)
        {
            return GetMessage(content, GetSampleHeaders());
        }

        public static HttpResponseMessage GetMessage(HttpContent content, string headers)
        {
            var httpResponseMessage = new HttpResponseMessage();
            httpResponseMessage.Headers.Parse(headers);
            httpResponseMessage.Content = content;
            return httpResponseMessage;
        }

        private static string GetSampleHeaders()
        {
            if(_sampleHeaders == null)
            {
                var manifestResourceStream =
                    Assembly.GetExecutingAssembly().GetManifestResourceStream(HeadersResourcePath);
                var bytes = new byte[manifestResourceStream.Length];
                manifestResourceStream.Read(bytes, 0, bytes.Length);
                _sampleHeaders = Encoding.UTF8.GetString(bytes);
            }
            return _sampleHeaders;
        }
    }
}