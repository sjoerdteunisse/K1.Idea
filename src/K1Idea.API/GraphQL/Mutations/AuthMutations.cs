using K1Idea.Application.Auth.Commands;
using K1Idea.Application.Auth.DTOs;
using MediatR;

namespace K1Idea.API.GraphQL.Mutations;

[MutationType]
public static class AuthMutations
{
    public static Task<AuthPayload> RegisterAsync(
        RegisterInput input,
        IMediator mediator,
        CancellationToken ct) =>
        mediator.Send(new RegisterCommand(input.Email, input.Password, input.DisplayName, input.TenantSlug), ct);

    public static Task<AuthPayload> LoginAsync(
        LoginInput input,
        IMediator mediator,
        CancellationToken ct) =>
        mediator.Send(new LoginCommand(input.Email, input.Password, input.TenantSlug), ct);

    public static Task<AuthPayload> RefreshTokenAsync(
        string token,
        IMediator mediator,
        CancellationToken ct) =>
        mediator.Send(new RefreshTokenCommand(token), ct);

    public static Task<AuthPayload> SelectOrganizationAsync(
        Guid orgId,
        IMediator mediator,
        CancellationToken ct) =>
        mediator.Send(new SelectOrganizationCommand(orgId), ct);
}

public sealed record RegisterInput(string Email, string Password, string DisplayName, string TenantSlug);
public sealed record LoginInput(string Email, string Password, string TenantSlug);
