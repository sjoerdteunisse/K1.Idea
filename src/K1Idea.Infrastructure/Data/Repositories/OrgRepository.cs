using Dapper;
using K1Idea.Domain.Entities;
using K1Idea.Domain.Interfaces;

namespace K1Idea.Infrastructure.Data.Repositories;

public sealed class OrgRepository(IUnitOfWork uow) : IOrgRepository
{
    public async Task<Tenant?> GetTenantBySlugAsync(string slug, CancellationToken ct)
    {
        const string sql = "SELECT * FROM tenants WHERE slug = @slug";
        return await uow.Connection.QuerySingleOrDefaultAsync<Tenant>(
            new CommandDefinition(sql, new { slug }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Organization>> ListByUserAsync(Guid userId, CancellationToken ct)
    {
        const string sql = """
            SELECT o.* FROM organizations o
            JOIN org_users ou ON ou.org_id = o.id
            WHERE ou.user_id = @userId
            ORDER BY o.name
            """;

        var result = await uow.Connection.QueryAsync<Organization>(
            new CommandDefinition(sql, new { userId }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return result.AsList();
    }

    public async Task<OrgUser?> GetOrgUserAsync(Guid orgId, Guid userId, CancellationToken ct)
    {
        const string sql = "SELECT * FROM org_users WHERE org_id = @orgId AND user_id = @userId";
        return await uow.Connection.QuerySingleOrDefaultAsync<OrgUser>(
            new CommandDefinition(sql, new { orgId, userId }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task InsertOrgUserAsync(OrgUser orgUser, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO org_users (id, tenant_id, org_id, user_id, role, created_at)
            VALUES (@Id, @TenantId, @OrgId, @UserId, @Role, @CreatedAt)
            """;

        await uow.Connection.ExecuteAsync(
            new CommandDefinition(sql, orgUser, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
    }
}
