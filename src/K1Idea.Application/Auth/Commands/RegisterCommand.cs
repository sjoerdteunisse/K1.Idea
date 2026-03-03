using FluentValidation;
using K1Idea.Application.Auth.DTOs;
using K1Idea.Application.Common.Exceptions;
using K1Idea.Application.Common.Interfaces;
using K1Idea.Domain.Entities;
using K1Idea.Domain.Interfaces;
using MediatR;

namespace K1Idea.Application.Auth.Commands;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string DisplayName,
    string TenantSlug) : IRequest<AuthPayload>;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TenantSlug).NotEmpty().MaximumLength(100);
    }
}

public sealed class RegisterCommandHandler(
    IUnitOfWork uow,
    IUserRepository users,
    IOrgRepository orgs,
    IPasswordHasher hasher,
    IJwtService jwt,
    IClock clock) : IRequestHandler<RegisterCommand, AuthPayload>
{
    public async Task<AuthPayload> Handle(RegisterCommand request, CancellationToken ct)
    {
        var tenant = await orgs.GetTenantBySlugAsync(request.TenantSlug, ct).ConfigureAwait(false)
            ?? throw new NotFoundException($"Tenant '{request.TenantSlug}' not found.");

        var existing = await users.GetByEmailAsync(request.Email.ToLowerInvariant(), ct).ConfigureAwait(false);
        if (existing is not null)
            throw new UnauthorizedException("Email already registered.");

        await uow.BeginAsync(ct).ConfigureAwait(false);
        try
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email.ToLowerInvariant(),
                DisplayName = request.DisplayName,
                PasswordHash = hasher.Hash(request.Password),
                CreatedAt = clock.UtcNow,
                UpdatedAt = clock.UtcNow,
            };
            await users.InsertAsync(user, ct).ConfigureAwait(false);

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
