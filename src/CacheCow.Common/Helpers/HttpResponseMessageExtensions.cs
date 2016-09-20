namespace CacheCow.Common.Helpers
{
    using System.Net.Http;
    using System.Threading.Tasks;

    public static class HttpResponseMessageExtensions
    {
        public static Task<HttpResponseMessage> ToTask(this HttpResponseMessage responseMessage)
        {
            var taskCompletionSource = new TaskCompletionSource<HttpResponseMessage>();
            taskCompletionSource.SetResult(responseMessage);
            return taskCompletionSource.Task;
        }
    }
}