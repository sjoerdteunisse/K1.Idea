using HotChocolate.Subscriptions;
using HotChocolate.Types;
using K1Idea.Domain.Entities;

namespace K1Idea.API.GraphQL.Subscriptions;

[SubscriptionType]
public static class CommentSubscriptions
{
    [Subscribe(With = nameof(SubscribeToCommentAddedAsync))]
    public static Comment CommentAdded([EventMessage] Comment comment) => comment;

    public static async IAsyncEnumerable<Comment> SubscribeToCommentAddedAsync(
        Guid ticketId,
        [Service] ITopicEventReceiver receiver,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var stream = await receiver.SubscribeAsync<Comment>($"comment_added_{ticketId}", ct).ConfigureAwait(false);
        await foreach (var comment in stream.ReadEventsAsync().WithCancellation(ct).ConfigureAwait(false))
            yield return comment;
    }
}
