using FluentValidation;
using K1Idea.Application.Auth.DTOs;
using K1Idea.Application.Common.Exceptions;
using K1Idea.Application.Common.Interfaces;
using K1Idea.Domain.Entities;
using K1Idea.Domain.Interfaces;
using MediatR;

namespace K1Idea.Application.Auth.Commands;

public sealed record RefreshTokenCommand(string Token) : IRequest<AuthPayload>;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
    }
}

public sealed class RefreshTokenCommandHandler(
    IUnitOfWork uow,
    IUserRepository users,
    IJwtService jwt,
    IClock clock) : IRequestHandler<RefreshTokenCommand, AuthPayload>
{
    public async Task<AuthPayload> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var stored = await users.GetRefreshTokenAsync(request.Token, ct).ConfigureAwait(false);
        if (stored is null || stored.RevokedAt.HasValue || stored.ExpiresAt < clock.UtcNow)
            throw new UnauthorizedException("Invalid or expired refresh token.");

        var user = await users.GetByIdAsync(stored.UserId, ct).ConfigureAwait(false)
            ?? throw new UnauthorizedException("User not found.");

        await uow.BeginAsync(ct).ConfigureAwait(false);
        try
        {
            await users.RevokeRefreshTokenAsync(request.Token, clock.UtcNow, ct).ConfigureAwait(false);

            var (newTokenValue, expiresAt) = jwt.GenerateRefreshToken();
            var newRefresh = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = newTokenValue,
                TenantId = stored.TenantId,
                OrgId = stored.OrgId,
                ExpiresAt = expiresAt,
                CreatedAt = clock.UtcNow,
            };
            await users.InsertRefreshTokenAsync(newRefresh, ct).ConfigureAwait(false);
            await uow.CommitAsync(ct).ConfigureAwait(false);

            var accessToken = jwt.GenerateAccessToken(user.Id, stored.TenantId, stored.OrgId);
            return new AuthPayload(accessToken, newTokenValue, user);
        }
        catch
        {
            await uow.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }
}
