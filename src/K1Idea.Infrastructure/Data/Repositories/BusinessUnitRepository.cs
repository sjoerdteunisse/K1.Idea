using Dapper;
using K1Idea.Domain.Entities;
using K1Idea.Domain.Interfaces;

namespace K1Idea.Infrastructure.Data.Repositories;

public sealed class BusinessUnitRepository(IUnitOfWork uow) : IBusinessUnitRepository
{
    public async Task<BusinessUnit?> GetByIdAsync(Guid tenantId, Guid orgId, Guid id, CancellationToken ct)
    {
        const string sql = """
            SELECT * FROM business_units
             WHERE id = @id AND tenant_id = @tenantId AND org_id = @orgId
            """;

        return await uow.Connection.QuerySingleOrDefaultAsync<BusinessUnit>(
            new CommandDefinition(sql, new { tenantId, orgId, id }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<BusinessUnit>> ListByOrgAsync(Guid tenantId, Guid orgId, CancellationToken ct)
    {
        const string sql = "SELECT * FROM business_units WHERE tenant_id = @tenantId AND org_id = @orgId ORDER BY name";
        var result = await uow.Connection.QueryAsync<BusinessUnit>(
            new CommandDefinition(sql, new { tenantId, orgId }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return result.AsList();
    }

    public async Task<IReadOnlyList<BusinessUnit>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        const string sql = "SELECT * FROM business_units WHERE id = ANY(@ids)";
        var result = await uow.Connection.QueryAsync<BusinessUnit>(
            new CommandDefinition(sql, new { ids = ids.ToArray() }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return result.AsList();
    }

    public async Task<IReadOnlyList<Guid>> GetBusinessUnitIdsForUserAsync(Guid orgId, Guid userId, CancellationToken ct)
    {
        const string sql = """
            SELECT business_unit_id FROM business_unit_users
             WHERE org_id = @orgId AND user_id = @userId
            """;

        var result = await uow.Connection.QueryAsync<Guid>(
            new CommandDefinition(sql, new { orgId, userId }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return result.AsList();
    }
}
