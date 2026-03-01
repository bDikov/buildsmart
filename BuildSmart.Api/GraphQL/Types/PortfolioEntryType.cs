using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Api.GraphQL.Types;

public class PortfolioEntryType : ObjectType<PortfolioEntry>
{
    protected override void Configure(IObjectTypeDescriptor<PortfolioEntry> descriptor)
    {
        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
        descriptor.Field(p => p.Title).Type<NonNullType<StringType>>();
        descriptor.Field(p => p.ImageUrl).Type<NonNullType<StringType>>();
        descriptor.Field(p => p.VideoUrl).Type<StringType>();
    }
}
