using K1Idea.Domain.Entities;

namespace K1Idea.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<IReadOnlyList<User>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct);
    Task<Guid> InsertAsync(User user, CancellationToken ct);

    Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken ct);
    Task InsertRefreshTokenAsync(RefreshToken token, CancellationToken ct);
    Task RevokeRefreshTokenAsync(string token, DateTimeOffset revokedAt, CancellationToken ct);
}
