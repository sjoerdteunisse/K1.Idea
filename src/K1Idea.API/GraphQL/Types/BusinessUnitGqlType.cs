using HotChocolate.Types;
using K1Idea.Domain.Entities;

namespace K1Idea.API.GraphQL.Types;

public sealed class BusinessUnitGqlType : ObjectType<BusinessUnit>
{
    protected override void Configure(IObjectTypeDescriptor<BusinessUnit> descriptor)
    {
        descriptor.Ignore(x => x.TenantId);
        descriptor.Ignore(x => x.OrgId);
        descriptor.Ignore(x => x.CreatedAt);
    }
}