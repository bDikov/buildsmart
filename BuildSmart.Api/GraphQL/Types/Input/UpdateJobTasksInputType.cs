using BuildSmart.Api.DTOs;
using HotChocolate.Types;

namespace BuildSmart.Api.GraphQL.Types.Input;

public class UpdateJobTasksInputType : InputObjectType<UpdateJobTasksInput>
{
    protected override void Configure(IInputObjectTypeDescriptor<UpdateJobTasksInput> descriptor)
    {
        descriptor.Description("Input for updating the standardized tasks of a job post.");

        descriptor.Field(t => t.JobPostId).Type<NonNullType<UuidType>>();
        descriptor.Field(t => t.Tasks)
            .Type<NonNullType<ListType<NonNullType<JobTaskInputType>>>>();
    }
}

public class JobTaskInputType : InputObjectType<JobTaskInput>
{
    protected override void Configure(IInputObjectTypeDescriptor<JobTaskInput> descriptor)
    {
        descriptor.Description("Input for a specific standardized job task.");

        descriptor.Field(t => t.Id).Type<UuidType>();
        descriptor.Field(t => t.Title).Type<NonNullType<StringType>>();
        descriptor.Field(t => t.Description).Type<NonNullType<StringType>>();
        descriptor.Field(t => t.SequenceOrder).Type<NonNullType<IntType>>();
        
        descriptor.Field(t => t.Criteria)
            .Type<NonNullType<ListType<NonNullType<TaskAcceptanceCriteriaInputType>>>>();
    }
}

public class TaskAcceptanceCriteriaInputType : InputObjectType<TaskAcceptanceCriteriaInput>
{
    protected override void Configure(IInputObjectTypeDescriptor<TaskAcceptanceCriteriaInput> descriptor)
    {
        descriptor.Description("Input for an acceptance criterion.");

        descriptor.Field(t => t.Id).Type<UuidType>();
        descriptor.Field(t => t.Description).Type<NonNullType<StringType>>();
    }
}