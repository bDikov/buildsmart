using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Api.GraphQL.Types;

public class ReviewType : ObjectType<Review>
{
	protected override void Configure(IObjectTypeDescriptor<Review> descriptor)
	{
		descriptor.Description("Represents a review of a tradesman's service by a homeowner.");

		descriptor.Field(r => r.Id).Type<NonNullType<IdType>>();
		descriptor.Field(r => r.Rating).Type<NonNullType<IntType>>();
		descriptor.Field(r => r.Comment).Type<StringType>();
		descriptor.Field(r => r.BookingId).Type<NonNullType<IdType>>();
		descriptor.Field(r => r.HomeownerId).Type<NonNullType<IdType>>();
		descriptor.Field(r => r.TradesmanProfileId).Type<NonNullType<IdType>>();

		// Relationships will be configured here later
	}
}