using BuildSmart.Api.GraphQL.Types;
using Microsoft.AspNetCore.Authorization;

namespace BuildSmart.Api.GraphQL;

[Authorize] // This applies to all fields in QueryType by default
public class QueryType : ObjectType<Query>
{
	private static readonly string[] AdminRoles = new[] { "Admin", "ADMIN", "admin" };
	private static readonly string[] TradesmanRoles = new[] { "Tradesman", "TRADESMAN", "tradesman", "Admin", "ADMIN", "admin" };
	private static readonly string[] HomeownerRoles = new[] { "Homeowner", "HOMEOWNER", "homeowner", "Admin", "ADMIN", "admin" };
	private static readonly string[] AllRoles = new[] { "Homeowner", "HOMEOWNER", "homeowner", "Tradesman", "TRADESMAN", "tradesman", "Admin", "ADMIN", "admin" };

	protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
	{
		descriptor.Description("The root query object.");

		descriptor.Field(q => q.GetFeedMedia(default!))
			.Description("Gets a queryable list of active media for the public feed.")
			.AllowAnonymous();

		descriptor.Field(q => q.GetTradesmanProfiles(default!))
			.Description("Gets a queryable list of tradesman profiles.")
			.Authorize(roles: AllRoles);

		descriptor.Field(q => q.GetCurrentUser(default!, default!))
			.Description("Gets the currently authenticated user.")
			.Type<UserType>()
			.Authorize(roles: AllRoles);

        descriptor.Field(q => q.GetServiceCategories(default!, default!))
            .Description("Gets a list of all active service categories.")
            .AllowAnonymous();

        descriptor.Field(q => q.GetAllServiceCategories(default!, default!))
            .Description("Gets all service categories, regardless of status. (Admin only)")
            .Authorize(roles: AdminRoles);

        descriptor.Field(q => q.GetMyProjects(default!, default!))
            .Description("Gets the projects created by or assigned to the authenticated user.")
            .Authorize(roles: AllRoles);

        descriptor.Field(q => q.GetJobPostsForReview(default!))
            .Authorize(roles: AdminRoles);

                descriptor.Field(q => q.GetProjectsForReview(default!))
                    .Authorize(roles: AdminRoles);

                descriptor.Field(q => q.GetUsers(default!))
                    .Description("Gets a list of all users. (Admin only)")
                    .Type<ListType<UserType>>()
                    .Authorize(roles: AdminRoles);
        
                descriptor.Field(q => q.GetMyNotifications(default!, default!))
                    .Description("Gets all notifications for the current user.")
                    .Authorize();

                descriptor.Field(q => q.GetAvailableAuctions(default!, default!, default!))
                    .Description("Gets all open auctions that match the authenticated tradesman's skills.")
                    .Type<ListType<AuctionType>>()
                    .Authorize(roles: TradesmanRoles);

                descriptor.Field(q => q.GetAuctionById(default!, default!))
                    .Description("Gets a specific auction by Job ID.")
                    .Type<AuctionType>()
                    .Authorize();

                descriptor.Field(q => q.GetJobPostQuestionById(default!, default!))
                    .Description("Gets a specific job post question by ID.")
                    .Type<JobPostQuestionType>()
                    .Authorize();

                descriptor.Field(q => q.GetProjectMessages(default!, default!, default!, default!, default!))
                    .Description("Gets paginated project messages for homeowners or admins.")
                    .Authorize();

                descriptor.Field(q => q.GetActiveSupportChats(default!, default!))
                    .Description("Gets all active project support chats for the admin support dashboard.")
                    .Authorize(roles: AdminRoles);

				descriptor.Field(q => q.GetQuestions(default!, default!, default!))
					.Description("Gets all questionnaire questions.")
					.Authorize(roles: AdminRoles);

				descriptor.Field(q => q.GetFormulas(default!, default!))
					.Description("Gets all pricing formulas.")
					.Authorize(roles: AdminRoles);

				descriptor.Field(q => q.GetQuestionGraph(default!, default!))
					.Description("Gets the full node-edge question flow and linkage graph.")
					.Authorize(roles: AdminRoles);

				descriptor.Field(q => q.GetLocalizationStrings(default!, default!))
					.Description("Gets all localization resources for a given culture. (Anonymous)")
					.AllowAnonymous();

				descriptor.Field(q => q.GetAllLocalizationResources(default!))
					.Description("Gets all localization resources across all cultures. (Admin only)")
					.Authorize(roles: AdminRoles);

				descriptor.Field(q => q.GetCalculatorLeads(default!))
					.Description("Gets all calculator leads for lead magnet monitoring. (Admin only)")
					.Authorize(roles: AdminRoles);
        	}
        }
        