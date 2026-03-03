using System.Text;

namespace K1Idea.Application.Common.Pagination;

public static class Cursor
{
    public static string Encode(DateTimeOffset createdAt, Guid id) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{createdAt:o}|{id}"));

    public static (DateTimeOffset? CreatedAt, Guid? Id) Decode(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor)) return (null, null);
        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = raw.Split('|');
            if (parts.Length != 2) return (null, null);
            return (DateTimeOffset.Parse(parts[0]), Guid.Parse(parts[1]));
        }
        catch
        {
            return (null, null);
        }
    }
}
