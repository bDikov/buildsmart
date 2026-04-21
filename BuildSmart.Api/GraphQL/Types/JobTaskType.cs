using BuildSmart.Core.Domain.Entities;
using HotChocolate.Types;

namespace BuildSmart.Api.GraphQL.Types;

public class JobTaskType : ObjectType<JobTask>
{
    protected override void Configure(IObjectTypeDescriptor<JobTask> descriptor)
    {
        descriptor.Description("Represents a specific task or phase within a job post.");

        descriptor.Field(t => t.Id).Type<NonNullType<UuidType>>();
        descriptor.Field(t => t.JobPostId).Type<NonNullType<UuidType>>();
        descriptor.Field(t => t.Title).Type<NonNullType<StringType>>();
        descriptor.Field(t => t.Description).Type<StringType>();
        descriptor.Field(t => t.SequenceOrder).Type<NonNullType<IntType>>();
        descriptor.Field(t => t.EstimatedPrice).Type<NonNullType<DecimalType>>();

        descriptor.Field(t => t.AcceptanceCriteria)
            .Type<NonNullType<ListType<NonNullType<TaskAcceptanceCriteriaType>>>>()
            .ResolveWith<JobTaskResolvers>(r => r.GetAcceptanceCriteria(default!, default!));

        descriptor.Field(t => t.SkuItems)
            .Type<NonNullType<ListType<NonNullType<TaskSkuItemType>>>>()
            .ResolveWith<JobTaskResolvers>(r => r.GetSkuItems(default!, default!));

        descriptor.Field(t => t.BidItems)
            .Type<NonNullType<ListType<NonNullType<BidItemType>>>>()
            .ResolveWith<JobTaskResolvers>(r => r.GetBidItems(default!, default!));

        descriptor.Field(t => t.Questions)
            .Type<NonNullType<ListType<NonNullType<JobPostQuestionType>>>>()
            .ResolveWith<JobTaskResolvers>(r => r.GetQuestions(default!, default!));
    }

    private class JobTaskResolvers
    {
        public IEnumerable<TaskAcceptanceCriteria> GetAcceptanceCriteria([Parent] JobTask jobTask, [Service] BuildSmart.Infrastructure.Persistence.AppDbContext dbContext)
        {
            return dbContext.TaskAcceptanceCriteria.Where(c => c.JobTaskId == jobTask.Id);
        }

        public IEnumerable<TaskSkuItem> GetSkuItems([Parent] JobTask jobTask, [Service] BuildSmart.Infrastructure.Persistence.AppDbContext dbContext)
        {
            return dbContext.TaskSkuItems.Where(s => s.JobTaskId == jobTask.Id);
        }

        public IEnumerable<BidItem> GetBidItems([Parent] JobTask jobTask, [Service] BuildSmart.Infrastructure.Persistence.AppDbContext dbContext)
        {
            return dbContext.BidItems.Where(b => b.JobTaskId == jobTask.Id);
        }

        public IEnumerable<JobPostQuestion> GetQuestions([Parent] JobTask jobTask, [Service] BuildSmart.Infrastructure.Persistence.AppDbContext dbContext)
        {
            return dbContext.JobPostQuestions.Where(q => q.JobTaskId == jobTask.Id);
        }
    }
}