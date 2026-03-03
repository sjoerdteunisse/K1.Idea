using K1Idea.Application.Common.Pagination;
using K1Idea.Application.Common.Tenancy;
using K1Idea.Domain.Entities;
using K1Idea.Domain.Interfaces;
using MediatR;

namespace K1Idea.Application.Comments.Queries;

public sealed record ListCommentsQuery(Guid TicketId, int First, string? After) : IRequest<Connection<Comment>>;

public sealed class ListCommentsQueryHandler(
    ICommentRepository comments,
    TenantContext tenantCtx,
    OrgContext orgCtx) : IRequestHandler<ListCommentsQuery, Connection<Comment>>
{
    public async Task<Connection<Comment>> Handle(ListCommentsQuery request, CancellationToken ct)
    {
        var rows = await comments.ListByTicketAsync(
            tenantCtx.TenantId, orgCtx.OrgId, request.TicketId, request.First, request.After, ct).ConfigureAwait(false);

        var hasNextPage = rows.Count > request.First;
        var nodes = hasNextPage ? rows.Take(request.First).ToList() : rows.ToList();
        var endCursor = nodes.Count > 0 ? Cursor.Encode(nodes[^1].CreatedAt, nodes[^1].Id) : null;

        return new Connection<Comment>(nodes.Count, new PageInfo(hasNextPage, endCursor), nodes);
    }
}
