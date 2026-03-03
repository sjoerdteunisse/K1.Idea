namespace K1Idea.Domain.Entities;

public sealed class Organization
{
    public required Guid Id { get; init; }
    public required Guid TenantId { get; init; }
    public required string Slug { get; init; }
    public required string Name { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
