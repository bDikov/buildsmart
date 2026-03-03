using BuildSmart.Core.Domain.Entities;
using System.Security.Claims;

namespace BuildSmart.Api.GraphQL.Types;

public class JobPostFeedbackType : ObjectType<JobPostFeedback>
{
    protected override void Configure(IObjectTypeDescriptor<JobPostFeedback> descriptor)
    {
        descriptor.Field(f => f.Id).Type<IdType>();
        descriptor.Field(f => f.JobPostId).Type<IdType>();
        descriptor.Field(f => f.JobPost).Type<JobPostType>();
        descriptor.Field(f => f.AuthorId).Type<IdType>();
        descriptor.Field(f => f.Author).Type<UserType>();
        descriptor.Field(f => f.Text).Type<NonNullType<StringType>>();
        descriptor.Field(f => f.IsResolved).Type<NonNullType<BooleanType>>();
        descriptor.Field(f => f.IsEdited).Type<NonNullType<BooleanType>>();
        descriptor.Field(f => f.CreatedAt).Type<NonNullType<DateTimeType>>();
        descriptor.Field(f => f.UpdatedAt).Type<NonNullType<DateTimeType>>();
        descriptor.Field(f => f.ParentFeedbackId).Type<IdType>();
        descriptor.Field(f => f.ParentFeedback).Type<JobPostFeedbackType>();
        descriptor.Field(f => f.Replies).Type<NonNullType<ListType<NonNullType<JobPostFeedbackType>>>>();

        // Computed field to check if the current user is the author
        descriptor.Field("isEditable")
            .Type<NonNullType<BooleanType>>()
            .Resolve(context =>
            {
                var feedback = context.Parent<JobPostFeedback>();
                var httpContext = context.Service<IHttpContextAccessor>().HttpContext;
                var claimsPrincipal = httpContext?.User;
                
                if (claimsPrincipal != null)
                {
                    var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? 
                                     claimsPrincipal.FindFirst("sub") ?? 
                                     claimsPrincipal.FindFirst("nameid");
                                     
                    if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                    {
                        return feedback.AuthorId == userId;
                    }
                }
                return false;
            });
    }
}
