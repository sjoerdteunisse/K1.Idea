using HotChocolate.Types;
using K1Idea.Domain.Entities;

namespace K1Idea.API.GraphQL.Types;

public sealed class UserGqlType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Ignore(x => x.PasswordHash);
    }
}
