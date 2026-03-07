using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Application.Interfaces;
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

		descriptor.Field("replies")
			.Argument("offset", a => a.Type<IntType>().DefaultValue(0))
			.Argument("limit", a => a.Type<IntType>().DefaultValue(5))
			.Type<NonNullType<ListType<NonNullType<JobPostQuestionType>>>>()
			.Resolve(async context =>
			{
				var question = context.Parent<JobPostQuestion>();
				var offset = context.ArgumentValue<int>("offset");
				var limit = context.ArgumentValue<int>("limit");
				var service = context.Service<IJobPostService>();

				return await service.GetQuestionRepliesAsync(question.Id, offset, limit);
			});

		descriptor.Field("replyCount")
			.Type<NonNullType<IntType>>()
			.Resolve(async context =>
			{
				var question = context.Parent<JobPostQuestion>();
				var service = context.Service<IJobPostService>();

				var dataLoader = context.BatchDataLoader<Guid, int>(
					async (keys, ct) =>
					{
						var counts = await service.GetQuestionReplyCountsBatchAsync(keys);
						// Ensure all requested keys have a value, defaulting to 0
						return keys.ToDictionary(k => k, k => counts.TryGetValue(k, out var count) ? count : 0);
					});

				return await dataLoader.LoadAsync(question.Id, context.RequestAborted);
			});

		// Computed field to check if the current user is the author of the question/reply        
		descriptor.Field("isEditable")
		        .Type<NonNullType<BooleanType>>()
		        .Resolve(context =>
		        {
		                var question = context.Parent<JobPostQuestion>();

		                // Get ClaimsPrincipal from either HttpContext or ContextData
		                var httpContext = context.Service<IHttpContextAccessor>().HttpContext;    
		                var claimsPrincipal = httpContext?.User ??
		                                                         (context.ContextData.TryGetValue("ClaimsPrincipal", out var p) ? p as ClaimsPrincipal : null);

		                if (claimsPrincipal != null)
		                {
		                        var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ??
		                                                         claimsPrincipal.FindFirst("sub") ??
		                                                         claimsPrincipal.FindFirst("nameid") ??
		                                                         claimsPrincipal.Claims.FirstOrDefault(c => c.Type.EndsWith("nameidentifier")) ??
		                                                         claimsPrincipal.Claims.FirstOrDefault(c => c.Type.EndsWith("sub"));

		                        if (userIdClaim != null)
		                        {
		                                var currentUserIdStr = userIdClaim.Value.Replace("-", "").ToLower();

		                                if (question.AuthorId != null)
		                                {
		                                        var authorIdStr = question.AuthorId.ToString()?.Replace("-", "").ToLower();
		                                        if (authorIdStr == currentUserIdStr) return true;
		                                }

		                                if (question.TradesmanProfile?.UserId != null)
		                                {
		                                        var tradesmanUserIdStr = question.TradesmanProfile.UserId.ToString()?.Replace("-", "").ToLower();
		                                        if (tradesmanUserIdStr == currentUserIdStr) return true;
		                                }
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
				var claimsPrincipal = httpContext?.User ??
		(context.ContextData.TryGetValue("ClaimsPrincipal", out var p) ? p as ClaimsPrincipal : null);

				if (claimsPrincipal != null)
				{
					var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ??
		claimsPrincipal.FindFirst("sub") ??
		claimsPrincipal.FindFirst("nameid") ??
		claimsPrincipal.Claims.FirstOrDefault(c => c.Type.EndsWith("nameidentifier")) ??
		claimsPrincipal.Claims.FirstOrDefault(c => c.Type.EndsWith("sub"));

					if (userIdClaim != null)
					{
						// Check if question has a job post and project, and user is the project owner
						if (question.JobPost?.Project?.HomeownerId != null)
						{
							var currentUserIdStr = userIdClaim.Value.Replace("-", "").ToLower();
							var ownerIdStr = question.JobPost.Project.HomeownerId.ToString()?.Replace("-", "").ToLower();
							return ownerIdStr == currentUserIdStr;
						}
					}
				}
				return false;
			});
	}
}