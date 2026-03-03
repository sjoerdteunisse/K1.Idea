using System.Text;
using Dapper;
using K1Idea.Domain.Interfaces;

namespace K1Idea.Infrastructure.Data.Sql;

public static class TicketSqlBuilder
{
    private static readonly HashSet<string> AllowedSortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "created_at", "updated_at", "priority"
    };

    private static readonly HashSet<string> AllowedDirections = new(StringComparer.OrdinalIgnoreCase)
    {
        "ASC", "DESC"
    };

    public static (string Sql, DynamicParameters Params) BuildListQuery(
        Guid tenantId,
        Guid orgId,
        IReadOnlyList<Guid> visibleBuIds,
        TicketListFilter filter,
        TicketListSort sort,
        TicketListPaging paging)
    {
        var (whereSql, p) = BuildWhere(tenantId, orgId, visibleBuIds, filter, paging);
        var orderSql = BuildOrder(sort);
        var sql = $"""
            SELECT DISTINCT t.*
            FROM tickets t
            JOIN ticket_business_units tbu ON tbu.ticket_id = t.id
            {whereSql}
            ORDER BY {orderSql}
            LIMIT @limit
            """;
        p.Add("limit", paging.First + 1);
        return (sql, p);
    }

    public static (string Sql, DynamicParameters Params) BuildCountQuery(
        Guid tenantId,
        Guid orgId,
        IReadOnlyList<Guid> visibleBuIds,
        TicketListFilter filter)
    {
        var paging = new TicketListPaging(int.MaxValue, null);
        var (whereSql, p) = BuildWhere(tenantId, orgId, visibleBuIds, filter, paging);
        var sql = $"""
            SELECT COUNT(DISTINCT t.id)
            FROM tickets t
            JOIN ticket_business_units tbu ON tbu.ticket_id = t.id
            {whereSql}
            """;
        return (sql, p);
    }

    private static (string Sql, DynamicParameters Params) BuildWhere(
        Guid tenantId,
        Guid orgId,
        IReadOnlyList<Guid> visibleBuIds,
        TicketListFilter filter,
        TicketListPaging paging)
    {
        var sb = new StringBuilder("WHERE t.tenant_id = @tenantId AND t.org_id = @orgId AND t.deleted_at IS NULL");
        var p = new DynamicParameters();
        p.Add("tenantId", tenantId);
        p.Add("orgId", orgId);

        sb.Append(" AND tbu.business_unit_id = ANY(@buIds)");
        p.Add("buIds", visibleBuIds.ToArray());

        if (!string.IsNullOrEmpty(filter.Type))
        {
            sb.Append(" AND t.type = @type");
            p.Add("type", filter.Type);
        }
        if (!string.IsNullOrEmpty(filter.Status))
        {
            sb.Append(" AND t.status = @status");
            p.Add("status", filter.Status);
        }
        if (!string.IsNullOrEmpty(filter.Priority))
        {
            sb.Append(" AND t.priority = @priority");
            p.Add("priority", filter.Priority);
        }
        if (filter.OwnerBusinessUnitId.HasValue)
        {
            sb.Append(" AND t.owner_business_unit_id = @ownerBuId");
            p.Add("ownerBuId", filter.OwnerBusinessUnitId.Value);
        }
        if (filter.AssigneeId.HasValue)
        {
            sb.Append(" AND t.assignee_id = @assigneeId");
            p.Add("assigneeId", filter.AssigneeId.Value);
        }
        if (!string.IsNullOrEmpty(filter.Text))
        {
            sb.Append(" AND t.title ILIKE @text");
            p.Add("text", $"%{filter.Text}%");
        }

        var (cursorCreatedAt, cursorId) = CursorPagingSql.Decode(paging.After);
        if (cursorCreatedAt.HasValue && cursorId.HasValue)
        {
            sb.Append(" AND (t.created_at, t.id) < (@cursorCreatedAt, @cursorId)");
            p.Add("cursorCreatedAt", cursorCreatedAt.Value);
            p.Add("cursorId", cursorId.Value);
        }

        return (sb.ToString(), p);
    }

    private static string BuildOrder(TicketListSort sort)
    {
        var field = AllowedSortFields.Contains(sort.Field) ? sort.Field : "created_at";
        var dir = AllowedDirections.Contains(sort.Direction) ? sort.Direction.ToUpperInvariant() : "DESC";
        return $"t.{field} {dir}, t.id {dir}";
    }
}
