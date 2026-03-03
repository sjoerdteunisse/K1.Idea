using System.Security.Claims;
using K1Idea.Application.Common.Tenancy;

namespace K1Idea.API.Middleware;

public sealed class CurrentUserMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, OrgContext orgCtx)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirstValue("sub");
            if (Guid.TryParse(sub, out var userId))
                orgCtx.UserId = userId;
        }
        await next(context).ConfigureAwait(false);
    }
}
