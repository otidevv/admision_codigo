namespace ADMISION.Extensions
{
    public static class HttpRequestExtensions
    {
        public static bool IsAjaxRequest(this Microsoft.AspNetCore.Http.HttpRequest request)
        {
            return string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
                || request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);
        }
    }
}
