using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Api.GraphQL.Types;

public class BookingType : ObjectType<Booking>
{
	protected override void Configure(IObjectTypeDescriptor<Booking> descriptor)
	{
		descriptor.Description("Represents a booking for a service from a homeowner to a tradesman.");

		descriptor.Field(b => b.Id).Type<NonNullType<IdType>>();
		descriptor.Field(b => b.RequestedDate).Type<NonNullType<DateTimeType>>();
		descriptor.Field(b => b.ScheduledDate).Type<DateTimeType>();
		descriptor.Field(b => b.JobDescription).Type<StringType>();
		descriptor.Field(b => b.Status).Type<NonNullType<EnumType<BuildSmart.Core.Domain.Enums.BookingStatusTypes>>>();
		descriptor.Field(b => b.HomeownerId).Type<NonNullType<IdType>>();
		descriptor.Field(b => b.TradesmanProfileId).Type<NonNullType<IdType>>();

		// Relationships will be configured here later
	}
}