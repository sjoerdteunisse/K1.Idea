namespace K1Idea.Application.Tickets.DTOs;

public sealed record TicketFilter(
    string? Type,
    string? Status,
    string? Priority,
    Guid? OwnerBusinessUnitId,
    Guid? AssigneeId,
    string? Text);
