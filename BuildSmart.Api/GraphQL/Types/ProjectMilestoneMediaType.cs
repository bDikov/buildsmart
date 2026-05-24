using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Api.GraphQL.Types;

public class ProjectMilestoneMediaType : ObjectType<ProjectMilestoneMedia>
{
    protected override void Configure(IObjectTypeDescriptor<ProjectMilestoneMedia> descriptor)
    {
        descriptor.Description("Represents raw verification photos or videos linked to a specific completed project/milestone.");

        descriptor.Field(t => t.Id).Type<NonNullType<IdType>>();
        descriptor.Field(t => t.TradesmanProfileId).Type<NonNullType<IdType>>();
        descriptor.Field(t => t.JobId).Type<NonNullType<IdType>>();
        descriptor.Field(t => t.Url).Type<NonNullType<StringType>>();
        descriptor.Field(t => t.Type).Type<NonNullType<StringType>>();
        descriptor.Field(t => t.CreatedAt).Type<NonNullType<DateTimeType>>();
    }
}