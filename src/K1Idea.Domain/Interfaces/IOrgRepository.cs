using K1Idea.Domain.Entities;

namespace K1Idea.Domain.Interfaces;

public interface IOrgRepository
{
    Task<Tenant?> GetTenantBySlugAsync(string slug, CancellationToken ct);
    Task<IReadOnlyList<Organization>> ListByUserAsync(Guid userId, CancellationToken ct);
    Task<OrgUser?> GetOrgUserAsync(Guid orgId, Guid userId, CancellationToken ct);
    Task InsertOrgUserAsync(OrgUser orgUser, CancellationToken ct);
}
