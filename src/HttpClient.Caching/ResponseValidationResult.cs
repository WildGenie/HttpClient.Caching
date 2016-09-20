namespace HttpClient.Caching
{
    public enum ResponseValidationResult
    {
        None,
        NotExist,
        OK,
        Stale,
        MustRevalidate,
        NotCacheable
    }
}