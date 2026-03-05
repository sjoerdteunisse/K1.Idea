using K1Idea.Domain.Enums;

namespace K1Idea.API.GraphQL.Types;

public sealed record TicketFilterInput(
    TicketType? Type,
    TicketStatus? Status,
    TicketPriority? Priority,
    Guid? OwnerBusinessUnitId,
    Guid? AssigneeId,
    string? Text);