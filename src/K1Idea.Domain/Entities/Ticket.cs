using K1Idea.Domain.Enums;

namespace K1Idea.Domain.Entities;

public sealed class Ticket
{
    public required Guid Id { get; init; }
    public required Guid TenantId { get; init; }
    public required Guid OrgId { get; init; }
    public required Guid OwnerBusinessUnitId { get; init; }

    public required string Title { get; set; }
    public string? Description { get; set; }

    public required Guid ReporterId { get; init; }
    public Guid? AssigneeId { get; set; }

    public required TicketType Type { get; init; }
    public required TicketStatus Status { get; set; }
    public required TicketPriority Priority { get; set; }

    public Guid? ParentId { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
