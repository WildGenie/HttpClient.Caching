namespace CacheCow.Client
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