using K1Idea.Domain.Entities;

namespace K1Idea.Domain.Interfaces;

public interface IBusinessUnitRepository
{
    Task<BusinessUnit?> GetByIdAsync(Guid tenantId, Guid orgId, Guid id, CancellationToken ct);
    Task<IReadOnlyList<BusinessUnit>> ListByOrgAsync(Guid tenantId, Guid orgId, CancellationToken ct);
    Task<IReadOnlyList<BusinessUnit>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct);
    Task<IReadOnlyList<Guid>> GetBusinessUnitIdsForUserAsync(Guid orgId, Guid userId, CancellationToken ct);
}
