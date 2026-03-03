using K1Idea.Application.Common.Pagination;

namespace K1Idea.Infrastructure.Data.Sql;

// Thin wrapper — delegates to Application.Common.Pagination.Cursor
public static class CursorPagingSql
{
    public static (DateTimeOffset? CreatedAt, Guid? Id) Decode(string? cursor) =>
        Cursor.Decode(cursor);

    public static string Encode(DateTimeOffset createdAt, Guid id) =>
        Cursor.Encode(createdAt, id);
}
