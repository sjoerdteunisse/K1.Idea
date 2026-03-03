namespace K1Idea.Domain.Entities;

public sealed class TicketBusinessUnit
{
    public required Guid Id { get; init; }
    public required Guid TenantId { get; init; }
    public required Guid OrgId { get; init; }
    public required Guid TicketId { get; init; }
    public required Guid BusinessUnitId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
