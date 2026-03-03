namespace K1Idea.Domain.Entities;

public sealed class Comment
{
    public required Guid Id { get; init; }
    public required Guid TenantId { get; init; }
    public required Guid OrgId { get; init; }
    public required Guid TicketId { get; init; }
    public required Guid AuthorId { get; init; }
    public required string Body { get; set; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; set; }
}
