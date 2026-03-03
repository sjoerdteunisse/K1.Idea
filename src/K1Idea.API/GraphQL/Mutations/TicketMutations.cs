using K1Idea.Application.Tickets.Commands;
using K1Idea.Application.Tickets.DTOs;
using K1Idea.Domain.Entities;
using K1Idea.Domain.Enums;
using MediatR;

namespace K1Idea.API.GraphQL.Mutations;

[MutationType]
public static class TicketMutations
{
    public static Task<Ticket> CreateTicketAsync(
        CreateTicketInput input,
        IMediator mediator,
        CancellationToken ct) =>
        mediator.Send(new CreateTicketCommand(
            input.Title,
            input.Description,
            input.Type,
            input.Priority,
            input.Status ?? TicketStatus.Backlog,
            input.ParentId,
            input.AssigneeId,
            input.OwnerBusinessUnitId,
            input.ShareWithBusinessUnitIds ?? []), ct);

    public static Task<Ticket> UpdateTicketAsync(
        Guid id,
        UpdateTicketInput input,
        IMediator mediator,
        CancellationToken ct) =>
        mediator.Send(new UpdateTicketCommand(
            id,
            input.Title,
            input.Description,
            input.Status,
            input.Priority,
            input.AssigneeId), ct);

    public static Task<bool> DeleteTicketAsync(
        Guid id,
        IMediator mediator,
        CancellationToken ct) =>
        mediator.Send(new DeleteTicketCommand(id), ct);

    public static Task<bool> ShareTicketAsync(
        Guid ticketId,
        IReadOnlyList<Guid> businessUnitIds,
        IMediator mediator,
        CancellationToken ct) =>
        mediator.Send(new ShareTicketCommand(ticketId, businessUnitIds), ct);
}

public sealed record CreateTicketInput(
    string Title,
    string? Description,
    TicketType Type,
    TicketPriority Priority,
    TicketStatus? Status,
    Guid? ParentId,
    Guid? AssigneeId,
    Guid OwnerBusinessUnitId,
    IReadOnlyList<Guid>? ShareWithBusinessUnitIds);

public sealed record UpdateTicketInput(
    string? Title,
    string? Description,
    TicketStatus? Status,
    TicketPriority? Priority,
    Guid? AssigneeId);
