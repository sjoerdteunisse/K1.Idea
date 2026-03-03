using FluentValidation;
using K1Idea.Application.Auth.DTOs;
using K1Idea.Application.Common.Exceptions;
using K1Idea.Application.Common.Interfaces;
using K1Idea.Domain.Entities;
using K1Idea.Domain.Interfaces;
using MediatR;

namespace K1Idea.Application.Auth.Commands;

public sealed record LoginCommand(
    string Email,
    string Password,
    string TenantSlug) : IRequest<AuthPayload>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.TenantSlug).NotEmpty();
    }
}

public sealed class LoginCommandHandler(
    IUnitOfWork uow,
    IUserRepository users,
    IOrgRepository orgs,
    IPasswordHasher hasher,
    IJwtService jwt,
    IClock clock) : IRequestHandler<LoginCommand, AuthPayload>
{
    public async Task<AuthPayload> Handle(LoginCommand request, CancellationToken ct)
    {
        var tenant = await orgs.GetTenantBySlugAsync(request.TenantSlug, ct).ConfigureAwait(false);
        if (tenant is null)
            throw new UnauthorizedException("Invalid credentials.");

        var user = await users.GetByEmailAsync(request.Email.ToLowerInvariant(), ct).ConfigureAwait(false);
        if (user is null || !hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid credentials.");

        await uow.BeginAsync(ct).ConfigureAwait(false);
        try
        {
            var (refreshTokenValue, expiresAt) = jwt.GenerateRefreshToken();
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshTokenValue,
                TenantId = tenant.Id,
                OrgId = null,
                ExpiresAt = expiresAt,
                CreatedAt = clock.UtcNow,
            };
            await users.InsertRefreshTokenAsync(refreshToken, ct).ConfigureAwait(false);
            await uow.CommitAsync(ct).ConfigureAwait(false);

            var accessToken = jwt.GenerateAccessToken(user.Id, tenant.Id, null);
            return new AuthPayload(accessToken, refreshTokenValue, user);
        }
        catch
        {
            await uow.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }
}
