using K1Idea.Application.Common.Pagination;
using K1Idea.Application.Common.Tenancy;
using K1Idea.Application.Tickets.DTOs;
using K1Idea.Domain.Entities;
using K1Idea.Domain.Interfaces;
using MediatR;

namespace K1Idea.Application.Tickets.Queries;

public sealed record ListTicketsQuery(
    int First,
    string? After,
    TicketFilter? Filter,
    TicketSort? Sort) : IRequest<Connection<Ticket>>;

public sealed class ListTicketsQueryHandler(
    ITicketRepository tickets,
    IBusinessUnitRepository bus,
    TenantContext tenantCtx,
    OrgContext orgCtx) : IRequestHandler<ListTicketsQuery, Connection<Ticket>>
{
    public async Task<Connection<Ticket>> Handle(ListTicketsQuery request, CancellationToken ct)
    {
        var visibleBuIds = await bus.GetBusinessUnitIdsForUserAsync(orgCtx.OrgId, orgCtx.UserId, ct).ConfigureAwait(false);
        var filter = request.Filter ?? new TicketFilter(null, null, null, null, null, null);
        var sort = request.Sort ?? new TicketSort();

        var repoFilter = new TicketListFilter(filter.Type, filter.Status, filter.Priority,
            filter.OwnerBusinessUnitId, filter.AssigneeId, filter.Text);
        var repoSort = new TicketListSort(sort.Field, sort.Direction);
        var repoPaging = new TicketListPaging(request.First, request.After);

        var rows = await tickets.ListScopedAsync(
            tenantCtx.TenantId, orgCtx.OrgId, visibleBuIds, repoFilter, repoSort, repoPaging, ct).ConfigureAwait(false);

        var hasNextPage = rows.Count > request.First;
        var nodes = hasNextPage ? rows.Take(request.First).ToList() : rows.ToList();

        var endCursor = nodes.Count > 0
            ? Cursor.Encode(nodes[^1].CreatedAt, nodes[^1].Id)
            : null;

        var totalCount = await tickets.CountScopedAsync(
            tenantCtx.TenantId, orgCtx.OrgId, visibleBuIds, repoFilter, ct).ConfigureAwait(false);

        return new Connection<Ticket>(totalCount, new PageInfo(hasNextPage, endCursor), nodes);
    }
}
