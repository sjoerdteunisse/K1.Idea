using FluentValidation;
using K1Idea.Application.Auth.DTOs;
using K1Idea.Application.Common.Exceptions;
using K1Idea.Application.Common.Interfaces;
using K1Idea.Application.Common.Tenancy;
using K1Idea.Domain.Entities;
using K1Idea.Domain.Interfaces;
using MediatR;

namespace K1Idea.Application.Auth.Commands;

public sealed record SelectOrganizationCommand(Guid OrgId) : IRequest<AuthPayload>;

public sealed class SelectOrganizationCommandValidator : AbstractValidator<SelectOrganizationCommand>
{
    public SelectOrganizationCommandValidator()
    {
        RuleFor(x => x.OrgId).NotEmpty();
    }
}

public sealed class SelectOrganizationCommandHandler(
    IUnitOfWork uow,
    IUserRepository users,
    IOrgRepository orgs,
    IJwtService jwt,
    IClock clock,
    TenantContext tenantCtx,
    OrgContext orgCtx) : IRequestHandler<SelectOrganizationCommand, AuthPayload>
{
    public async Task<AuthPayload> Handle(SelectOrganizationCommand request, CancellationToken ct)
    {
        var orgUser = await orgs.GetOrgUserAsync(request.OrgId, orgCtx.UserId, ct).ConfigureAwait(false)
            ?? throw new ForbiddenException("User is not a member of that organization.");

        var user = await users.GetByIdAsync(orgCtx.UserId, ct).ConfigureAwait(false)
            ?? throw new NotFoundException("User not found.");

        await uow.BeginAsync(ct).ConfigureAwait(false);
        try
        {
            var (newTokenValue, expiresAt) = jwt.GenerateRefreshToken();
            var newRefresh = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = newTokenValue,
                TenantId = tenantCtx.TenantId,
                OrgId = request.OrgId,
                ExpiresAt = expiresAt,
                CreatedAt = clock.UtcNow,
            };
            await users.InsertRefreshTokenAsync(newRefresh, ct).ConfigureAwait(false);
            await uow.CommitAsync(ct).ConfigureAwait(false);

            var accessToken = jwt.GenerateAccessToken(user.Id, tenantCtx.TenantId, request.OrgId);
            return new AuthPayload(accessToken, newTokenValue, user);
        }
        catch
        {
            await uow.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }
}
