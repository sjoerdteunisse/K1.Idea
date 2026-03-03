using K1Idea.Application.Comments.Queries;
using K1Idea.Application.Common.Pagination;
using K1Idea.Domain.Entities;
using MediatR;

namespace K1Idea.API.GraphQL.Queries;

[QueryType]
public static class CommentQueries
{
    public static Task<Connection<Comment>> CommentsAsync(
        Guid ticketId,
        int first,
        string? after,
        IMediator mediator,
        CancellationToken ct) =>
        mediator.Send(new ListCommentsQuery(ticketId, first, after), ct);
}
