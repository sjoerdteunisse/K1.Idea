using Dapper;
using K1Idea.Domain.Entities;
using K1Idea.Domain.Interfaces;

namespace K1Idea.Infrastructure.Data.Repositories;

public sealed class UserRepository(IUnitOfWork uow) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        const string sql = "SELECT * FROM users WHERE id = @id";
        return await uow.Connection.QuerySingleOrDefaultAsync<User>(
            new CommandDefinition(sql, new { id }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        const string sql = "SELECT * FROM users WHERE email = @email";
        return await uow.Connection.QuerySingleOrDefaultAsync<User>(
            new CommandDefinition(sql, new { email }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<User>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        const string sql = "SELECT * FROM users WHERE id = ANY(@ids)";
        var result = await uow.Connection.QueryAsync<User>(
            new CommandDefinition(sql, new { ids = ids.ToArray() }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return result.AsList();
    }

    public async Task<Guid> InsertAsync(User user, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO users (id, email, display_name, password_hash, created_at, updated_at)
            VALUES (@Id, @Email, @DisplayName, @PasswordHash, @CreatedAt, @UpdatedAt)
            RETURNING id
            """;

        return await uow.Connection.ExecuteScalarAsync<Guid>(
            new CommandDefinition(sql, user, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken ct)
    {
        const string sql = "SELECT * FROM refresh_tokens WHERE token = @token";
        return await uow.Connection.QuerySingleOrDefaultAsync<RefreshToken>(
            new CommandDefinition(sql, new { token }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task InsertRefreshTokenAsync(RefreshToken token, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO refresh_tokens (id, user_id, token, tenant_id, org_id, expires_at, created_at)
            VALUES (@Id, @UserId, @Token, @TenantId, @OrgId, @ExpiresAt, @CreatedAt)
            """;

        await uow.Connection.ExecuteAsync(
            new CommandDefinition(sql, token, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task RevokeRefreshTokenAsync(string token, DateTimeOffset revokedAt, CancellationToken ct)
    {
        const string sql = "UPDATE refresh_tokens SET revoked_at = @revokedAt WHERE token = @token";
        await uow.Connection.ExecuteAsync(
            new CommandDefinition(sql, new { token, revokedAt }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
    }
}
