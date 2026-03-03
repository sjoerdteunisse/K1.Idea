using K1Idea.Domain.Enums;

namespace K1Idea.Application.Tickets.DTOs;

public sealed record TicketDto(
    Guid Id,
    Guid TenantId,
    Guid OrgId,
    Guid OwnerBusinessUnitId,
    string Title,
    string? Description,
    Guid ReporterId,
    Guid? AssigneeId,
    TicketType Type,
    TicketStatus Status,
    TicketPriority Priority,
    Guid? ParentId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
