using SuperAppAPI.Extensions;
using SuperAppModels.Time;
using SuperAppServices.Time;

namespace SuperAppAPI.Middlewares
{
    /// <summary>
    /// Sets the ambient <see cref="UserClock"/> timezone for the request from the authenticated
    /// user's profile (urm.user_profiles.timezone). Must run after UseAuthentication().
    /// Anonymous requests use the default (Asia/Ho_Chi_Minh).
    /// </summary>
    public sealed class UserTimeZoneMiddleware
    {
        private readonly RequestDelegate _next;

        public UserTimeZoneMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            var timeZone = TimeZones.Default;

            if (context.User.IsAuthenticated()
                && int.TryParse(context.User.GetUserId(), out var userId))
            {
                var resolver = context.RequestServices.GetRequiredService<IUserTimeZoneResolver>();
                timeZone = await resolver.GetTimeZoneAsync(userId, context.RequestAborted);
            }

            using (UserClock.Use(timeZone))
            {
                await _next(context);
            }
        }
    }

    public static class UserTimeZoneMiddlewareExtensions
    {
        public static IApplicationBuilder UseUserTimeZone(this IApplicationBuilder app)
            => app.UseMiddleware<UserTimeZoneMiddleware>();
    }
}
