using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Api.GraphQL.Types;

public class JobPostType : ObjectType<JobPost>
{
    protected override void Configure(IObjectTypeDescriptor<JobPost> descriptor)
    {
        descriptor.Field(jp => jp.Feedbacks)
            .Resolve(async context =>
            {
                var jobPost = context.Parent<JobPost>();
                var service = context.Service<IJobPostService>();

                var dataLoader = context.BatchDataLoader<Guid, IEnumerable<JobPostFeedback>>(
                    async (keys, ct) => 
                    {
                        var lookup = await service.GetFeedbacksBatchByJobPostIdsAsync(keys);
                        return keys.ToDictionary(k => k, k => (IEnumerable<JobPostFeedback>)lookup[k]);
                    });

                return await dataLoader.LoadAsync(jobPost.Id, context.RequestAborted);
            });

        descriptor.Field(jp => jp.Bids)
            .Resolve(async context =>
            {
                var jobPost = context.Parent<JobPost>();
                var service = context.Service<IJobPostService>();

                var dataLoader = context.BatchDataLoader<Guid, IEnumerable<Bid>>(
                    async (keys, ct) => 
                    {
                        var lookup = await service.GetBidsBatchByJobPostIdsAsync(keys);
                        return keys.ToDictionary(k => k, k => (IEnumerable<Bid>)lookup[k]);
                    });

                return await dataLoader.LoadAsync(jobPost.Id, context.RequestAborted);
            });

        descriptor.Field(jp => jp.Questions)
            .Resolve(async context =>
            {
                var jobPost = context.Parent<JobPost>();
                var service = context.Service<IJobPostService>();

                var dataLoader = context.BatchDataLoader<Guid, IEnumerable<JobPostQuestion>>(
                    async (keys, ct) => 
                    {
                        var lookup = await service.GetQuestionsBatchByJobPostIdsAsync(keys);
                        return keys.ToDictionary(k => k, k => (IEnumerable<JobPostQuestion>)lookup[k]);
                    });

                return await dataLoader.LoadAsync(jobPost.Id, context.RequestAborted);
            });
    }
}
