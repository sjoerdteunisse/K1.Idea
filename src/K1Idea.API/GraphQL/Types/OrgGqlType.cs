using HotChocolate.Types;
using K1Idea.Domain.Entities;

namespace K1Idea.API.GraphQL.Types;

public sealed class OrgGqlType : ObjectType<Organization>
{
    protected override void Configure(IObjectTypeDescriptor<Organization> descriptor)
    {
        descriptor.Ignore(x => x.TenantId);
        descriptor.Ignore(x => x.CreatedAt);
    }
}