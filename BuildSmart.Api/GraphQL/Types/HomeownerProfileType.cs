using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Api.GraphQL.Types;

public class HomeownerProfileType : ObjectType<HomeownerProfile>
{
    protected override void Configure(IObjectTypeDescriptor<HomeownerProfile> descriptor)
    {
        descriptor.Description("Represents the specific profile for a homeowner.");

        descriptor.Field(h => h.Id).Type<NonNullType<IdType>>();
        descriptor.Field(h => h.UserId).Type<NonNullType<IdType>>();
        descriptor.Field(h => h.Address).Type<StringType>();
    }
}
