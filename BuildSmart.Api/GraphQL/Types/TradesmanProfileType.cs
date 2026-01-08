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

        descriptor.Field(t => t.Skills)
            .Description("The list of skills and service categories this tradesman offers.")
            .Type<NonNullType<ListType<NonNullType<TradesmanSkillType>>>>();

		// Relationships will be configured here later
	}
}