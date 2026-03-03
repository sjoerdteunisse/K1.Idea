using K1Idea.Application.Auth.Commands;
using K1Idea.Application.Common.Exceptions;
using K1Idea.Application.Common.Interfaces;
using K1Idea.Domain.Entities;
using K1Idea.Domain.Interfaces;

namespace K1Idea.Application.Tests.Auth;

public sealed class RegisterCommandHandlerTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IOrgRepository _orgs = Substitute.For<IOrgRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtService _jwt = Substitute.For<IJwtService>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private readonly Tenant _tenant = new()
    {
        Id = Guid.NewGuid(),
        Slug = "acme",
        Name = "ACME",
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private RegisterCommandHandler CreateHandler()
    {
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        _hasher.Hash(Arg.Any<string>()).Returns("hashed");
        _jwt.GenerateRefreshToken().Returns(("refresh-token", DateTimeOffset.UtcNow.AddDays(7)));
        _jwt.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<Guid?>()).Returns("access-token");
        return new RegisterCommandHandler(_uow, _users, _orgs, _hasher, _jwt, _clock);
    }

    [Fact]
    public async Task Handle_valid_command_returns_auth_payload()
    {
        _orgs.GetTenantBySlugAsync("acme", Arg.Any<CancellationToken>()).Returns(_tenant);
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        _users.InsertAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());

        var cmd = new RegisterCommand("test@example.com", "Password123!", "Test User", "acme");
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.User.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task Handle_unknown_tenant_throws_not_found()
    {
        _orgs.GetTenantBySlugAsync("unknown", Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        var cmd = new RegisterCommand("test@example.com", "Password123!", "Test User", "unknown");
        var act = () => CreateHandler().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_duplicate_email_throws_unauthorized()
    {
        _orgs.GetTenantBySlugAsync("acme", Arg.Any<CancellationToken>()).Returns(_tenant);
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            DisplayName = "Existing",
            PasswordHash = "x",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });

        var cmd = new RegisterCommand("test@example.com", "Password123!", "Test User", "acme");
        var act = () => CreateHandler().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
