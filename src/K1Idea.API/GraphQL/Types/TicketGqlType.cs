using HotChocolate;
using HotChocolate.Types;
using K1Idea.Application.Common.Pagination;
using K1Idea.Domain.Entities;

namespace K1Idea.API.GraphQL.Types;

[ExtendObjectType(typeof(Ticket))]
public sealed class TicketGqlType
{
    public async Task<BusinessUnit?> GetOwnerBusinessUnitAsync(
        [Parent] Ticket ticket,
        BusinessUnitByIdDataLoader loader,
        CancellationToken ct) =>
        await loader.LoadAsync(ticket.OwnerBusinessUnitId, ct).ConfigureAwait(false);

    public async Task<User?> GetReporterAsync(
        [Parent] Ticket ticket,
        UserByIdDataLoader loader,
        CancellationToken ct) =>
        await loader.LoadAsync(ticket.ReporterId, ct).ConfigureAwait(false);

    public async Task<User?> GetAssigneeAsync(
        [Parent] Ticket ticket,
        UserByIdDataLoader loader,
        CancellationToken ct) =>
        ticket.AssigneeId.HasValue
            ? await loader.LoadAsync(ticket.AssigneeId.Value, ct).ConfigureAwait(false)
            : null;

    public async Task<Ticket?> GetParentAsync(
        [Parent] Ticket ticket,
        TicketByIdDataLoader loader,
        CancellationToken ct) =>
        ticket.ParentId.HasValue
            ? await loader.LoadAsync(ticket.ParentId.Value, ct).ConfigureAwait(false)
            : null;
}
