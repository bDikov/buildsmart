using BuildSmart.Core.Domain.Entities;

// Corrected namespace
namespace BuildSmart.Api.GraphQL.Types;

public class UserType : ObjectType<User>
{
	protected override void Configure(IObjectTypeDescriptor<User> descriptor)
	{
		descriptor.Description("Represents a user of the application, who can be a homeowner or a tradesman.");

		descriptor.Field(u => u.Id).Type<NonNullType<IdType>>();
		descriptor.Field(u => u.FirstName).Type<NonNullType<StringType>>();
		descriptor.Field(u => u.LastName).Type<NonNullType<StringType>>();
		descriptor.Field(u => u.Email).Type<NonNullType<StringType>>();
		descriptor.Field(u => u.PhoneNumber).Type<StringType>();
		descriptor.Field(u => u.Role).Type<NonNullType<EnumType<BuildSmart.Core.Domain.Enums.UserRoleTypes>>>();
		descriptor.Field(u => u.Bio).Type<StringType>();
		descriptor.Field(u => u.Location).Type<StringType>();
		descriptor.Field(u => u.ProfilePictureUrl).Type<StringType>();

		descriptor.Field(u => u.HashedPassword).Ignore(); // Do not expose password hash

        descriptor.Field(u => u.HomeownerProfile).Type<HomeownerProfileType>();
        descriptor.Field(u => u.TradesmanProfile).Type<TradesmanProfileType>();

		// Relationships will be configured here later
	}
}