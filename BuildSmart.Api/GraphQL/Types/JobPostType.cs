using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Api.GraphQL.Types;

public class JobPostType : ObjectType<JobPost>
{
    protected override void Configure(IObjectTypeDescriptor<JobPost> descriptor)
    {
        descriptor.Field(jp => jp.Feedbacks).UseFiltering().UseSorting();
        descriptor.Field(jp => jp.Bids).UseFiltering().UseSorting();
    }
}
