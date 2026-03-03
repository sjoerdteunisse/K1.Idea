using K1Idea.Domain.Interfaces;
using Npgsql;

namespace K1Idea.Infrastructure.Data;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly NpgsqlConnectionFactory _factory;
    private NpgsqlConnection? _connection;
    private NpgsqlTransaction? _transaction;

    public UnitOfWork(NpgsqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public NpgsqlConnection Connection => _connection ?? throw new InvalidOperationException("Connection not opened.");
    public NpgsqlTransaction? Transaction => _transaction;

    public async Task BeginAsync(CancellationToken ct)
    {
        _connection = _factory.Create();
        await _connection.OpenAsync(ct).ConfigureAwait(false);
        _transaction = await _connection.BeginTransactionAsync(ct).ConfigureAwait(false);
    }

    public async Task CommitAsync(CancellationToken ct)
    {
        if (_transaction is null) throw new InvalidOperationException("No active transaction.");
        await _transaction.CommitAsync(ct).ConfigureAwait(false);
    }

    public async Task RollbackAsync(CancellationToken ct)
    {
        if (_transaction is not null)
            await _transaction.RollbackAsync(ct).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync().ConfigureAwait(false);
            _transaction = null;
        }
        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
            _connection = null;
        }
    }
}
