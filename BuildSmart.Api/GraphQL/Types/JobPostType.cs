using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Api.GraphQL.DataLoaders;

namespace BuildSmart.Api.GraphQL.Types;

public class JobPostType : ObjectType<JobPost>
{
	protected override void Configure(IObjectTypeDescriptor<JobPost> descriptor)
	{
		descriptor.Field(jp => jp.Id)
			.IsProjected(true);

		descriptor.Field(jp => jp.Feedbacks)
			.Resolve(async context =>
			{
				var jobPost = context.Parent<JobPost>();
				return await context.DataLoader<FeedbacksByJobPostIdDataLoader>()
					.LoadAsync(jobPost.Id, context.RequestAborted);
			});

		descriptor.Field(jp => jp.Bids)
			.Resolve(async context =>
			{
				var jobPost = context.Parent<JobPost>();
				return await context.DataLoader<BidsByJobPostIdDataLoader>()
					.LoadAsync(jobPost.Id, context.RequestAborted);
			});

		descriptor.Field(jp => jp.JobTasks)
			.Resolve(async context =>
			{
				var jobPost = context.Parent<JobPost>();
				return await context.DataLoader<JobTasksByJobPostIdDataLoader>()
					.LoadAsync(jobPost.Id, context.RequestAborted);
			});

		descriptor.Field(jp => jp.Questions)
			.Resolve(async context =>
			{
				var jobPost = context.Parent<JobPost>();
				return await context.DataLoader<QuestionsByJobPostIdDataLoader>()
					.LoadAsync(jobPost.Id, context.RequestAborted);
			});
	}
}