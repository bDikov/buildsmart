using BuildSmart.Core.Domain.Entities;
using System.Security.Claims;

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
		descriptor.Field(q => q.ParentQuestionId).Type<IdType>();
		descriptor.Field(q => q.TradesmanProfileId).Type<IdType>();
		descriptor.Field(q => q.TradesmanProfile).Type<TradesmanProfileType>();
		descriptor.Field(q => q.AuthorId).Type<IdType>();
		descriptor.Field(q => q.Author).Type<UserType>();
		descriptor.Field(q => q.Replies).Type<NonNullType<ListType<NonNullType<JobPostQuestionType>>>>();

		// Computed field to check if the current user is the author of the question/reply
		descriptor.Field("isEditable")
			.Type<NonNullType<BooleanType>>()
			.Resolve(context =>
			{
				var question = context.Parent<JobPostQuestion>();
				var httpContext = context.Service<IHttpContextAccessor>().HttpContext;
				var claimsPrincipal = httpContext?.User;

				if (claimsPrincipal != null)
				{
					var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub");
					if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
					{
						return question.AuthorId == userId;
					}
				}
				return false;
			});

		// Computed field to check if the current user can edit the ANSWER (homeowner of the project)
		descriptor.Field("isAnswerEditable")
			.Type<NonNullType<BooleanType>>()
			.Resolve(context =>
			{
				var question = context.Parent<JobPostQuestion>();
				var httpContext = context.Service<IHttpContextAccessor>().HttpContext;
				var claimsPrincipal = httpContext?.User;

				if (claimsPrincipal != null)
				{
					var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub");
					if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
					{
						// Check if question has a job post and project, and user is the project owner
						if (question.JobPost?.Project != null)
						{
							return question.JobPost.Project.HomeownerId == userId;
						}
					}
				}
				return false;
			});
	}
}