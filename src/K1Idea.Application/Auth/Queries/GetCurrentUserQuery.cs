using K1Idea.Application.Common.Exceptions;
using K1Idea.Application.Common.Tenancy;
using K1Idea.Domain.Entities;
using K1Idea.Domain.Interfaces;
using MediatR;

namespace K1Idea.Application.Auth.Queries;

public sealed record GetCurrentUserQuery : IRequest<User>;

public sealed class GetCurrentUserQueryHandler(
    IUserRepository users,
    OrgContext orgCtx) : IRequestHandler<GetCurrentUserQuery, User>
{
    public async Task<User> Handle(GetCurrentUserQuery request, CancellationToken ct)
    {
        return await users.GetByIdAsync(orgCtx.UserId, ct).ConfigureAwait(false)
            ?? throw new NotFoundException($"User '{orgCtx.UserId}' not found.");
    }
}