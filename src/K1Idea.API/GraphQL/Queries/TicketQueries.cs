using K1Idea.Application.Common.Pagination;
using K1Idea.Application.Tickets.DTOs;
using K1Idea.Application.Tickets.Queries;
using K1Idea.Domain.Entities;
using MediatR;

namespace K1Idea.API.GraphQL.Queries;

[QueryType]
public static class TicketQueries
{
    public static Task<Ticket> TicketByIdAsync(
        Guid id,
        IMediator mediator,
        CancellationToken ct) =>
        mediator.Send(new GetTicketByIdQuery(id), ct);

    public static Task<Connection<Ticket>> TicketsAsync(
        int first,
        string? after,
        TicketFilterInput? where,
        TicketSortInput? order,
        IMediator mediator,
        CancellationToken ct)
    {
        var filter = where is null
            ? null
            : new TicketFilter(where.Type, where.Status, where.Priority,
                where.OwnerBusinessUnitId, where.AssigneeId, where.Text);

        var sort = order is null
            ? null
            : new TicketSort(order.Field ?? "created_at", order.Direction ?? "DESC");

        return mediator.Send(new ListTicketsQuery(first, after, filter, sort), ct);
    }
}

public sealed record TicketFilterInput(
    string? Type,
    string? Status,
    string? Priority,
    Guid? OwnerBusinessUnitId,
    Guid? AssigneeId,
    string? Text);

public sealed record TicketSortInput(string? Field, string? Direction);
