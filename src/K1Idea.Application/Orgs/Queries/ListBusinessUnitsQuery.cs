using K1Idea.Application.Common.Tenancy;
using K1Idea.Domain.Entities;
using K1Idea.Domain.Interfaces;
using MediatR;

namespace K1Idea.Application.Orgs.Queries;

public sealed record ListBusinessUnitsQuery : IRequest<IReadOnlyList<BusinessUnit>>;

public sealed class ListBusinessUnitsQueryHandler(
    IBusinessUnitRepository bus,
    TenantContext tenantCtx,
    OrgContext orgCtx) : IRequestHandler<ListBusinessUnitsQuery, IReadOnlyList<BusinessUnit>>
{
    public Task<IReadOnlyList<BusinessUnit>> Handle(ListBusinessUnitsQuery request, CancellationToken ct) =>
        bus.ListByOrgAsync(tenantCtx.TenantId, orgCtx.OrgId, ct);
}
