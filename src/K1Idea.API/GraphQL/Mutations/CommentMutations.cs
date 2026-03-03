using HotChocolate.Subscriptions;
using K1Idea.Application.Comments.Commands;
using K1Idea.Domain.Entities;
using MediatR;

namespace K1Idea.API.GraphQL.Mutations;

[MutationType]
public static class CommentMutations
{
    public static async Task<Comment> AddCommentAsync(
        Guid ticketId,
        string body,
        IMediator mediator,
        ITopicEventSender sender,
        CancellationToken ct)
    {
        var comment = await mediator.Send(new AddCommentCommand(ticketId, body), ct).ConfigureAwait(false);
        await sender.SendAsync($"comment_added_{ticketId}", comment, ct).ConfigureAwait(false);
        return comment;
    }

    public static Task<bool> DeleteCommentAsync(
        Guid commentId,
        IMediator mediator,
        CancellationToken ct) =>
        mediator.Send(new DeleteCommentCommand(commentId), ct);
}
