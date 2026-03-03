namespace K1Idea.Application.Common.Pagination;

public sealed record Connection<T>(
    int TotalCount,
    PageInfo PageInfo,
    IReadOnlyList<T> Nodes);
