using System.Security.Claims;
using K1Idea.Application.Common.Tenancy;

namespace K1Idea.API.Middleware;

public sealed class TenantOrgContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, TenantContext tenantCtx, OrgContext orgCtx)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = context.User.FindFirstValue("tenant_id");
            var orgClaim = context.User.FindFirstValue("org_id");

            if (Guid.TryParse(tenantClaim, out var tenantId))
                tenantCtx.TenantId = tenantId;
            if (Guid.TryParse(orgClaim, out var orgId))
                orgCtx.OrgId = orgId;
        }

        await next(context).ConfigureAwait(false);
    }
}
