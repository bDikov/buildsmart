using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Api.GraphQL.Types;

public class JobPostQuestionType : ObjectType<JobPostQuestion>
{
    protected override void Configure(IObjectTypeDescriptor<JobPostQuestion> descriptor)
    {
        descriptor.Field(q => q.Id).Type<NonNullType<IdType>>();
        descriptor.Field(q => q.QuestionText).Type<NonNullType<StringType>>();
        descriptor.Field(q => q.AnswerText).Type<StringType>();
        descriptor.Field(q => q.AnsweredAt).Type<DateTimeType>();
        descriptor.Field(q => q.IsAnswered).Type<NonNullType<BooleanType>>();
        descriptor.Field(q => q.IsEdited).Type<NonNullType<BooleanType>>();
        descriptor.Field(q => q.IsAnswerEdited).Type<NonNullType<BooleanType>>();
    }
}
