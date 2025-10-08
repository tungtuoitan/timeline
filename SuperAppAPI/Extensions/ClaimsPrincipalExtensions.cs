using System.Security.Claims;

namespace SuperAppAPI.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string? GetUserId(this ClaimsPrincipal principal)
        {
            return principal.FindFirstValue(ClaimTypes.NameIdentifier) 
                ?? principal.FindFirstValue("sub");
        }

        public static string? GetUserEmail(this ClaimsPrincipal principal)
        {
            return principal.FindFirstValue(ClaimTypes.Email) 
                ?? principal.FindFirstValue("email");
        }

        public static string? GetUserName(this ClaimsPrincipal principal)
        {
            return principal.FindFirstValue(ClaimTypes.Name) 
                ?? principal.FindFirstValue("name");
        }

        public static bool IsAuthenticated(this ClaimsPrincipal principal)
        {
            return principal.Identity?.IsAuthenticated ?? false;
        }
    }
}