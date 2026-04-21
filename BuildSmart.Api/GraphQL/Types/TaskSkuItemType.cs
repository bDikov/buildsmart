using BuildSmart.Core.Domain.Entities;
using HotChocolate.Types;

namespace BuildSmart.Api.GraphQL.Types;

public class TaskSkuItemType : ObjectType<TaskSkuItem>
{
    protected override void Configure(IObjectTypeDescriptor<TaskSkuItem> descriptor)
    {
        descriptor.Field(t => t.Id).Type<NonNullType<UuidType>>();
        descriptor.Field(t => t.JobTaskId).Type<NonNullType<UuidType>>();
        descriptor.Field(t => t.ServiceSkuId).Type<NonNullType<UuidType>>();
        descriptor.Field(t => t.Quantity).Type<NonNullType<DecimalType>>();
        descriptor.Field(t => t.EstimatedPrice).Type<NonNullType<DecimalType>>();
        descriptor.Field(t => t.CreatedAt).Type<NonNullType<DateTimeType>>();
        descriptor.Field(t => t.UpdatedAt).Type<NonNullType<DateTimeType>>();
        
        descriptor.Field(t => t.ServiceSku)
            .Type<NonNullType<ServiceSkuType>>()
            .ResolveWith<TaskSkuItemResolvers>(r => r.GetServiceSku(default!, default!));
    }

    private class TaskSkuItemResolvers
    {
        public ServiceSku GetServiceSku([Parent] TaskSkuItem item, [Service] BuildSmart.Infrastructure.Persistence.AppDbContext dbContext)
        {
            return dbContext.ServiceSkus.First(s => s.Id == item.ServiceSkuId);
        }
    }
}