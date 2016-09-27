namespace HttpClient.Caching.Cache
{
    public enum CacheStatus
    {
        CannotUseCache,
        Revalidate,
        ReturnStored
    }
}