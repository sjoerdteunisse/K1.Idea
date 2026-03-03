using K1Idea.Application.Common.Exceptions;
using K1Idea.Application.Common.Tenancy;
using K1Idea.Domain.Entities;
using K1Idea.Domain.Interfaces;
using MediatR;

namespace K1Idea.Application.Tickets.Queries;

public sealed record GetTicketByIdQuery(Guid TicketId) : IRequest<Ticket>;

public sealed class GetTicketByIdQueryHandler(
    ITicketRepository tickets,
    IBusinessUnitRepository bus,
    TenantContext tenantCtx,
    OrgContext orgCtx) : IRequestHandler<GetTicketByIdQuery, Ticket>
{
    public async Task<Ticket> Handle(GetTicketByIdQuery request, CancellationToken ct)
    {
        var visibleBuIds = await bus.GetBusinessUnitIdsForUserAsync(orgCtx.OrgId, orgCtx.UserId, ct).ConfigureAwait(false);

        return await tickets.GetByIdScopedAsync(
            tenantCtx.TenantId, orgCtx.OrgId, request.TicketId, visibleBuIds, ct).ConfigureAwait(false)
            ?? throw new NotFoundException($"Ticket '{request.TicketId}' not found.");
    }
}
