using K1Idea.Domain.Entities;

namespace K1Idea.Domain.Interfaces;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdScopedAsync(
        Guid tenantId,
        Guid orgId,
        Guid ticketId,
        IReadOnlyList<Guid> visibleBusinessUnitIds,
        CancellationToken ct);

    Task<IReadOnlyList<Ticket>> ListScopedAsync(
        Guid tenantId,
        Guid orgId,
        IReadOnlyList<Guid> visibleBusinessUnitIds,
        TicketListFilter filter,
        TicketListSort sort,
        TicketListPaging paging,
        CancellationToken ct);

    Task<int> CountScopedAsync(
        Guid tenantId,
        Guid orgId,
        IReadOnlyList<Guid> visibleBusinessUnitIds,
        TicketListFilter filter,
        CancellationToken ct);

    Task<Guid> InsertAsync(Ticket ticket, CancellationToken ct);
    Task UpdateAsync(Ticket ticket, CancellationToken ct);
    Task SoftDeleteAsync(Guid tenantId, Guid orgId, Guid ticketId, DateTimeOffset deletedAt, CancellationToken ct);

    Task<IReadOnlyList<TicketBusinessUnit>> GetBusinessUnitsForTicketAsync(Guid ticketId, CancellationToken ct);
    Task InsertTicketBusinessUnitAsync(TicketBusinessUnit tbu, CancellationToken ct);
    Task<IReadOnlyList<Ticket>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct);
}

public sealed record TicketListFilter(
    string? Type,
    string? Status,
    string? Priority,
    Guid? OwnerBusinessUnitId,
    Guid? AssigneeId,
    string? Text);

public sealed record TicketListSort(string Field, string Direction);

public sealed record TicketListPaging(int First, string? After);
