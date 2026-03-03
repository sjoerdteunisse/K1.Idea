using Dapper;
using K1Idea.Domain.Entities;
using K1Idea.Domain.Interfaces;
using K1Idea.Infrastructure.Data.Sql;

namespace K1Idea.Infrastructure.Data.Repositories;

public sealed class CommentRepository(IUnitOfWork uow) : ICommentRepository
{
    public async Task<Comment?> GetByIdAsync(Guid tenantId, Guid orgId, Guid commentId, CancellationToken ct)
    {
        const string sql = """
            SELECT * FROM comments
             WHERE id = @commentId AND tenant_id = @tenantId AND org_id = @orgId
            """;

        return await uow.Connection.QuerySingleOrDefaultAsync<Comment>(
            new CommandDefinition(sql, new { tenantId, orgId, commentId },
                transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Comment>> ListByTicketAsync(
        Guid tenantId,
        Guid orgId,
        Guid ticketId,
        int first,
        string? after,
        CancellationToken ct)
    {
        var (cursorCreatedAt, cursorId) = CursorPagingSql.Decode(after);

        var sql = cursorCreatedAt.HasValue
            ? """
              SELECT * FROM comments
               WHERE tenant_id = @tenantId AND org_id = @orgId AND ticket_id = @ticketId
                 AND (created_at, id) < (@cursorCreatedAt, @cursorId)
               ORDER BY created_at DESC, id DESC
               LIMIT @limit
              """
            : """
              SELECT * FROM comments
               WHERE tenant_id = @tenantId AND org_id = @orgId AND ticket_id = @ticketId
               ORDER BY created_at DESC, id DESC
               LIMIT @limit
              """;

        var result = await uow.Connection.QueryAsync<Comment>(
            new CommandDefinition(sql,
                new { tenantId, orgId, ticketId, limit = first + 1, cursorCreatedAt, cursorId },
                transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);

        return result.AsList();
    }

    public async Task<Guid> InsertAsync(Comment comment, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO comments (id, tenant_id, org_id, ticket_id, author_id, body, created_at, updated_at)
            VALUES (@Id, @TenantId, @OrgId, @TicketId, @AuthorId, @Body, @CreatedAt, @UpdatedAt)
            RETURNING id
            """;

        return await uow.Connection.ExecuteScalarAsync<Guid>(
            new CommandDefinition(sql, comment, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid tenantId, Guid orgId, Guid commentId, CancellationToken ct)
    {
        const string sql = """
            DELETE FROM comments WHERE id = @commentId AND tenant_id = @tenantId AND org_id = @orgId
            """;

        await uow.Connection.ExecuteAsync(
            new CommandDefinition(sql, new { tenantId, orgId, commentId },
                transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
    }
}
