using HotChocolate;
using HotChocolate.Types;
using K1Idea.Application.Common.Exceptions;
using K1Idea.Application.Common.Tenancy;
using K1Idea.Domain.Entities;
using K1Idea.Domain.Enums;
using K1Idea.Domain.Interfaces;

namespace K1Idea.API.GraphQL.Types;

[ExtendObjectType(typeof(User))]
public sealed class UserRoleExtension
{
    public async Task<UserRole> GetRoleAsync(
        [Parent] User user,
        IOrgRepository orgRepo,
        OrgContext orgCtx,
        CancellationToken ct)
    {
        var orgUser = await orgRepo.GetOrgUserAsync(orgCtx.OrgId, user.Id, ct).ConfigureAwait(false)
            ?? throw new NotFoundException($"User '{user.Id}' has no role in the current organization.");
        return orgUser.Role;
    }
}