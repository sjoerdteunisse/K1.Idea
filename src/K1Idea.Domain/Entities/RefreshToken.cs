namespace K1Idea.Domain.Entities;

public sealed class RefreshToken
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required string Token { get; init; }
    public Guid? TenantId { get; init; }
    public Guid? OrgId { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset? RevokedAt { get; set; }
    public required DateTimeOffset CreatedAt { get; init; }
}
