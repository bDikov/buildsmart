using BuildSmart.Core.Domain.Entities;
using HotChocolate.Types;

namespace BuildSmart.Api.GraphQL.Types;

public class TaskAcceptanceCriteriaType : ObjectType<TaskAcceptanceCriteria>
{
    protected override void Configure(IObjectTypeDescriptor<TaskAcceptanceCriteria> descriptor)
    {
        descriptor.Description("Represents a specific acceptance criterion for a job task.");

        descriptor.Field(c => c.Id).Type<NonNullType<UuidType>>();
        descriptor.Field(c => c.JobTaskId).Type<NonNullType<UuidType>>();
        descriptor.Field(c => c.Description).Type<NonNullType<StringType>>();
    }
}