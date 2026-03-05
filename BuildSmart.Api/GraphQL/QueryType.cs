using BuildSmart.Api.GraphQL.Types;
using Microsoft.AspNetCore.Authorization;

namespace BuildSmart.Api.GraphQL;

[Authorize] // This applies to all fields in QueryType by default
public class QueryType : ObjectType<Query>
{
	protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
	{
		descriptor.Description("The root query object.");

		descriptor.Field(q => q.GetTradesmanProfiles(default!))
			.Description("Gets a queryable list of tradesman profiles.")
			.Authorize(roles: new[] { "Homeowner", "Tradesman", "Admin" }); // Explicitly authorize for these roles

		descriptor.Field(q => q.GetCurrentUser(default!, default!))
			.Description("Gets the currently authenticated user.")
			.Type<UserType>()
			.Authorize(roles: new[] { "Homeowner", "Tradesman", "Admin" });

        descriptor.Field(q => q.GetServiceCategories(default!))
            .Description("Gets a list of all active service categories.");

        descriptor.Field(q => q.GetAllServiceCategories(default!))
            .Description("Gets all service categories, regardless of status. (Admin only)")
            .Authorize(roles: new[] { "Admin" });

        descriptor.Field(q => q.GetMyProjects(default!, default!))
            .Description("Gets the projects created by the authenticated homeowner.")
            .Authorize(roles: new[] { "Homeowner" });

        descriptor.Field(q => q.GetJobPostsForReview(default!))
            .Authorize(roles: new[] { "Admin" });

                descriptor.Field(q => q.GetProjectsForReview(default!))
                    .Authorize(roles: new[] { "Admin" });

                descriptor.Field(q => q.GetUsers(default!))
                    .Description("Gets a list of all users. (Admin only)")
                    .Type<ListType<UserType>>()
                    .Authorize(roles: new[] { "Admin" });
        
                descriptor.Field(q => q.GetMyNotifications(default!, default!))
                    .Description("Gets all notifications for the current user.")
                    .Authorize();

                descriptor.Field(q => q.GetAvailableAuctions(default!, default!, default!))
                    .Description("Gets all open auctions that match the authenticated tradesman's skills.")
                    .Type<ListType<AuctionType>>()
                    .Authorize(roles: new[] { "Tradesman" });

                descriptor.Field(q => q.GetAuctionById(default!, default!))
                    .Description("Gets a specific auction by Job ID.")
                    .Type<AuctionType>()
                    .Authorize();

                descriptor.Field(q => q.GetJobPostQuestionById(default!, default!))
                    .Description("Gets a specific job post question by ID.")
                    .Type<JobPostQuestionType>()
                    .Authorize();
        	}
        }
        