using BuildSmart.Core.Domain.Entities;
using HotChocolate.Types;

namespace BuildSmart.Api.GraphQL.Types;

public class ServiceSkuType : ObjectType<ServiceSku>
{
    protected override void Configure(IObjectTypeDescriptor<ServiceSku> descriptor)
    {
        descriptor.Description("Represents a billable SKU item for a specific service category.");

        descriptor.Field(s => s.Id).Type<NonNullType<UuidType>>();
        descriptor.Field(s => s.ServiceCategoryId).Type<NonNullType<UuidType>>();
        descriptor.Field(s => s.SkuCode).Type<NonNullType<StringType>>();
        descriptor.Field(s => s.Name).Type<NonNullType<StringType>>();
        descriptor.Field(s => s.Description).Type<StringType>();
        descriptor.Field(s => s.BasePrice).Type<NonNullType<DecimalType>>();
        descriptor.Field(s => s.UnitType).Type<NonNullType<StringType>>();
    }
}
