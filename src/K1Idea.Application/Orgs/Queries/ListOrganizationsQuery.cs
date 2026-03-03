using K1Idea.Application.Common.Tenancy;
using K1Idea.Domain.Entities;
using K1Idea.Domain.Interfaces;
using MediatR;

namespace K1Idea.Application.Orgs.Queries;

public sealed record ListOrganizationsQuery : IRequest<IReadOnlyList<Organization>>;

public sealed class ListOrganizationsQueryHandler(
    IOrgRepository orgs,
    OrgContext orgCtx) : IRequestHandler<ListOrganizationsQuery, IReadOnlyList<Organization>>
{
    public Task<IReadOnlyList<Organization>> Handle(ListOrganizationsQuery request, CancellationToken ct) =>
        orgs.ListByUserAsync(orgCtx.UserId, ct);
}
