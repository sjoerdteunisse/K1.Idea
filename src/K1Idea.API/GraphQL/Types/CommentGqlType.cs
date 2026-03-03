using HotChocolate;
using HotChocolate.Types;
using K1Idea.Domain.Entities;

namespace K1Idea.API.GraphQL.Types;

[ExtendObjectType(typeof(Comment))]
public sealed class CommentGqlType
{
    public async Task<User?> GetAuthorAsync(
        [Parent] Comment comment,
        UserByIdDataLoader loader,
        CancellationToken ct) =>
        await loader.LoadAsync(comment.AuthorId, ct).ConfigureAwait(false);
}
