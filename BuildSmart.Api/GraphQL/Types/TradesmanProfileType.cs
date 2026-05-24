using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Api.GraphQL.Types;

public class TradesmanProfileType : ObjectType<TradesmanProfile>
{
	protected override void Configure(IObjectTypeDescriptor<TradesmanProfile> descriptor)
	{
		descriptor.Description("Represents the specific profile for a tradesman, extending the base User entity with role-specific data.");

		descriptor.Field(t => t.Id).Type<NonNullType<IdType>>();
		descriptor.Field(t => t.UserId).Type<NonNullType<IdType>>();
		descriptor.Field(t => t.AverageRating).Type<NonNullType<FloatType>>();
		descriptor.Field(t => t.IsVerified).Type<NonNullType<BooleanType>>();
		descriptor.Field(t => t.VideoIntroductionUrl).Type<StringType>();

        descriptor.Field(t => t.Skills)
            .Description("The list of skills and service categories this tradesman offers.")
            .Type<NonNullType<ListType<NonNullType<TradesmanSkillType>>>>();

		descriptor.Field(t => t.PortfolioEntries)
			.Description("The tradesman's portfolio items.")
			.Type<NonNullType<ListType<NonNullType<PortfolioEntryType>>>>();

		descriptor.Field(t => t.Certifications)
			.Description("The tradesman's certifications and credentials.")
			.Type<NonNullType<ListType<NonNullType<CertificationType>>>>();

        descriptor.Field(t => t.Media)
            .Description("High-quality media files (Reels) associated with this tradesman's portfolio.")
            .Type<NonNullType<ListType<NonNullType<TradesmanMediaType>>>>();

        descriptor.Field(t => t.MilestoneMedia)
            .Description("Milestone verification media for completed projects.")
            .Type<NonNullType<ListType<NonNullType<ProjectMilestoneMediaType>>>>();

		// Relationships will be configured here later
	}
}