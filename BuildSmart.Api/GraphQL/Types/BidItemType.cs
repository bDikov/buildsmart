using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Application.Interfaces;
using HotChocolate.Types;

namespace BuildSmart.Api.GraphQL.Types;

public class BidItemType : ObjectType<BidItem>
{
    protected override void Configure(IObjectTypeDescriptor<BidItem> descriptor)
    {
        descriptor.Description("Represents a priced item within a bid, mapping to a specific JobTask.");

        descriptor.Field(bi => bi.Id).Type<NonNullType<UuidType>>();
        descriptor.Field(bi => bi.BidId).Type<NonNullType<UuidType>>();
        descriptor.Field(bi => bi.JobTaskId).Type<NonNullType<UuidType>>();
        
        descriptor.Field(bi => bi.Price)
            .Description("The price proposed for this task.")
            .Resolve(ctx => ctx.Parent<BidItem>().Price);

        descriptor.Field(bi => bi.Comment).Type<StringType>();

        descriptor.Field(bi => bi.JobTask)
            .Type<NonNullType<JobTaskType>>()
            .ResolveWith<BidItemResolvers>(r => r.GetJobTask(default!, default!));
    }

    private class BidItemResolvers
    {
        public JobTask GetJobTask([Parent] BidItem bidItem, [Service] IUnitOfWork unitOfWork)
        {
            return unitOfWork.JobTasks.GetQueryable().First(jt => jt.Id == bidItem.JobTaskId);
        }
    }
}