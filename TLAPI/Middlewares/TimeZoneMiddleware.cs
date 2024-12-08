namespace TLAPI.Middleware
{
    public class TimeZoneMiddleware
    {
        private readonly RequestDelegate _next;

        public TimeZoneMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            var timezone = httpContext.Request.Headers["X-Timezone"].ToString();

            if (!string.IsNullOrEmpty(timezone))
            {
                httpContext.Items["UserTimeZone"] = timezone;
            }

            await _next(httpContext);
        }
    }

}
