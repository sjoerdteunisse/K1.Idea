using Npgsql;

namespace K1Idea.Infrastructure.Data;

public sealed class NpgsqlConnectionFactory(string connectionString)
{
    public NpgsqlConnection Create() => new(connectionString);
}
