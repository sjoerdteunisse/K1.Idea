using K1Idea.Application.Orgs.Queries;
using K1Idea.Domain.Entities;
using MediatR;

namespace K1Idea.API.GraphQL.Queries;

[QueryType]
public static class OrgQueries
{
    public static Task<IReadOnlyList<Organization>> OrganizationsAsync(
        IMediator mediator,
        CancellationToken ct) =>
        mediator.Send(new ListOrganizationsQuery(), ct);

    public static Task<IReadOnlyList<BusinessUnit>> BusinessUnitsAsync(
        IMediator mediator,
        CancellationToken ct) =>
        mediator.Send(new ListBusinessUnitsQuery(), ct);
}
