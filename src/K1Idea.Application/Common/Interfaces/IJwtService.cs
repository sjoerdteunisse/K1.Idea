using System.Security.Claims;

namespace K1Idea.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(Guid userId, Guid? tenantId, Guid? orgId);
    (string Token, DateTimeOffset ExpiresAt) GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
}
