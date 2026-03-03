using GreenDonut;
using K1Idea.Domain.Entities;
using K1Idea.Domain.Interfaces;

namespace K1Idea.API.GraphQL;

public sealed class UserByIdDataLoader(
    IBatchScheduler scheduler,
    IUserRepository users,
    DataLoaderOptions options)
    : BatchDataLoader<Guid, User>(scheduler, options)
{
    protected override async Task<IReadOnlyDictionary<Guid, User>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken ct)
    {
        var result = await users.GetByIdsAsync(keys, ct).ConfigureAwait(false);
        return result.ToDictionary(u => u.Id);
    }
}

public sealed class BusinessUnitByIdDataLoader(
    IBatchScheduler scheduler,
    IBusinessUnitRepository bus,
    DataLoaderOptions options)
    : BatchDataLoader<Guid, BusinessUnit>(scheduler, options)
{
    protected override async Task<IReadOnlyDictionary<Guid, BusinessUnit>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken ct)
    {
        var result = await bus.GetByIdsAsync(keys, ct).ConfigureAwait(false);
        return result.ToDictionary(b => b.Id);
    }
}

public sealed class TicketByIdDataLoader(
    IBatchScheduler scheduler,
    ITicketRepository tickets,
    DataLoaderOptions options)
    : BatchDataLoader<Guid, Ticket>(scheduler, options)
{
    protected override async Task<IReadOnlyDictionary<Guid, Ticket>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken ct)
    {
        var result = await tickets.GetByIdsAsync(keys, ct).ConfigureAwait(false);
        return result.ToDictionary(t => t.Id);
    }
}
