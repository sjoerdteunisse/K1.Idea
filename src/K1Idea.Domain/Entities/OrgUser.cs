using K1Idea.Domain.Enums;

namespace K1Idea.Domain.Entities;

public sealed class OrgUser
{
    public required Guid Id { get; init; }
    public required Guid TenantId { get; init; }
    public required Guid OrgId { get; init; }
    public required Guid UserId { get; init; }
    public required UserRole Role { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
