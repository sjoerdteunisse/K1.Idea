using Dapper;
using K1Idea.Domain.Entities;
using K1Idea.Domain.Interfaces;
using K1Idea.Infrastructure.Data.Sql;

namespace K1Idea.Infrastructure.Data.Repositories;

public sealed class TicketRepository(IUnitOfWork uow) : ITicketRepository
{
    public async Task<Ticket?> GetByIdScopedAsync(
        Guid tenantId,
        Guid orgId,
        Guid ticketId,
        IReadOnlyList<Guid> visibleBusinessUnitIds,
        CancellationToken ct)
    {
        const string sql = """
            SELECT DISTINCT t.*
            FROM tickets t
            JOIN ticket_business_units tbu ON tbu.ticket_id = t.id
            WHERE t.id = @ticketId
              AND t.tenant_id = @tenantId
              AND t.org_id = @orgId
              AND t.deleted_at IS NULL
              AND tbu.business_unit_id = ANY(@buIds)
            """;

        return await uow.Connection.QuerySingleOrDefaultAsync<Ticket>(
            new CommandDefinition(sql, new { ticketId, tenantId, orgId, buIds = visibleBusinessUnitIds.ToArray() },
                transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Ticket>> ListScopedAsync(
        Guid tenantId,
        Guid orgId,
        IReadOnlyList<Guid> visibleBusinessUnitIds,
        TicketListFilter filter,
        TicketListSort sort,
        TicketListPaging paging,
        CancellationToken ct)
    {
        var (sql, p) = TicketSqlBuilder.BuildListQuery(tenantId, orgId, visibleBusinessUnitIds, filter, sort, paging);
        var result = await uow.Connection.QueryAsync<Ticket>(
            new CommandDefinition(sql, p, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return result.AsList();
    }

    public async Task<int> CountScopedAsync(
        Guid tenantId,
        Guid orgId,
        IReadOnlyList<Guid> visibleBusinessUnitIds,
        TicketListFilter filter,
        CancellationToken ct)
    {
        var (sql, p) = TicketSqlBuilder.BuildCountQuery(tenantId, orgId, visibleBusinessUnitIds, filter);
        return await uow.Connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, p, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task<Guid> InsertAsync(Ticket ticket, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO tickets
                (id, tenant_id, org_id, owner_business_unit_id,
                 title, description, reporter_id, assignee_id,
                 type, status, priority, parent_id,
                 created_at, updated_at)
            VALUES
                (@Id, @TenantId, @OrgId, @OwnerBusinessUnitId,
                 @Title, @Description, @ReporterId, @AssigneeId,
                 @Type, @Status, @Priority, @ParentId,
                 @CreatedAt, @UpdatedAt)
            RETURNING id
            """;

        return await uow.Connection.ExecuteScalarAsync<Guid>(
            new CommandDefinition(sql, ticket, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Ticket ticket, CancellationToken ct)
    {
        const string sql = """
            UPDATE tickets
               SET title = @Title, description = @Description,
                   status = @Status, priority = @Priority,
                   assignee_id = @AssigneeId, updated_at = @UpdatedAt
             WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId
            """;

        await uow.Connection.ExecuteAsync(
            new CommandDefinition(sql, ticket, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task SoftDeleteAsync(Guid tenantId, Guid orgId, Guid ticketId, DateTimeOffset deletedAt, CancellationToken ct)
    {
        const string sql = """
            UPDATE tickets SET deleted_at = @deletedAt
             WHERE id = @ticketId AND tenant_id = @tenantId AND org_id = @orgId
            """;

        await uow.Connection.ExecuteAsync(
            new CommandDefinition(sql, new { tenantId, orgId, ticketId, deletedAt },
                transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TicketBusinessUnit>> GetBusinessUnitsForTicketAsync(Guid ticketId, CancellationToken ct)
    {
        const string sql = "SELECT * FROM ticket_business_units WHERE ticket_id = @ticketId";
        var result = await uow.Connection.QueryAsync<TicketBusinessUnit>(
            new CommandDefinition(sql, new { ticketId }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return result.AsList();
    }

    public async Task InsertTicketBusinessUnitAsync(TicketBusinessUnit tbu, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO ticket_business_units (id, tenant_id, org_id, ticket_id, business_unit_id, created_at)
            VALUES (@Id, @TenantId, @OrgId, @TicketId, @BusinessUnitId, @CreatedAt)
            ON CONFLICT (ticket_id, business_unit_id) DO NOTHING
            """;

        await uow.Connection.ExecuteAsync(
            new CommandDefinition(sql, tbu, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Ticket>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        const string sql = "SELECT * FROM tickets WHERE id = ANY(@ids) AND deleted_at IS NULL";
        var result = await uow.Connection.QueryAsync<Ticket>(
            new CommandDefinition(sql, new { ids = ids.ToArray() }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return result.AsList();
    }
}
