using K1Idea.Domain.Entities;

namespace K1Idea.Domain.Interfaces;

public interface ICommentRepository
{
    Task<Comment?> GetByIdAsync(Guid tenantId, Guid orgId, Guid commentId, CancellationToken ct);

    Task<IReadOnlyList<Comment>> ListByTicketAsync(
        Guid tenantId,
        Guid orgId,
        Guid ticketId,
        int first,
        string? after,
        CancellationToken ct);

    Task<Guid> InsertAsync(Comment comment, CancellationToken ct);
    Task DeleteAsync(Guid tenantId, Guid orgId, Guid commentId, CancellationToken ct);
}
