namespace CacheCow.Client.Internal
{
    using System.Net.Http.Headers;

    internal static class CacheControlHeaderExtensions
    {
        public static bool ShouldRevalidate(this CacheControlHeaderValue headerValue, bool defaultBehaviour)
        {
            if(headerValue == null)
                return false;
            return defaultBehaviour || headerValue.MustRevalidate || headerValue.NoCache;
        }
    }
}