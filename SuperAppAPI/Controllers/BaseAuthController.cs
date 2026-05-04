using Microsoft.AspNetCore.Mvc;
using SuperAppAPI.Extensions;

namespace SuperAppAPI.Controllers
{
    public abstract class BaseAuthController : ControllerBase
    {
        protected int? GetUserId()
        {
            var claim = User.GetUserId();
            return string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id) ? null : id;
        }

        protected string? GetUserEmail() => User.GetUserEmail();
    }
}
