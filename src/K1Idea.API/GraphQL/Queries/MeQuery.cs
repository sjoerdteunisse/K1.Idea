using K1Idea.Application.Auth.Queries;
using K1Idea.Domain.Entities;
using MediatR;

namespace K1Idea.API.GraphQL.Queries;

[QueryType]
public static class MeQuery
{
    public static Task<User> MeAsync(
        IMediator mediator,
        CancellationToken ct) =>
        mediator.Send(new GetCurrentUserQuery(), ct);
}