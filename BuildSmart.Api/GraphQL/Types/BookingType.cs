using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Api.GraphQL.Types;

public class BookingType : ObjectType<Booking>
{
	protected override void Configure(IObjectTypeDescriptor<Booking> descriptor)
	{
		descriptor.Description("Represents a booking for a service from a homeowner to a tradesman.");

		descriptor.Field(b => b.Id).Type<NonNullType<IdType>>();
		descriptor.Field(b => b.JobPostId).Type<NonNullType<IdType>>();
		descriptor.Field(b => b.BidId).Type<NonNullType<IdType>>();
		descriptor.Field(b => b.Status).Type<NonNullType<EnumType<BuildSmart.Core.Domain.Enums.BookingStatusTypes>>>();
		descriptor.Field(b => b.HomeownerId).Type<NonNullType<IdType>>();
		descriptor.Field(b => b.TradesmanProfileId).Type<NonNullType<IdType>>();

		descriptor.Field(b => b.AgreedBidAmount);
		descriptor.Field(b => b.PlatformFeeHomeowner);
		descriptor.Field(b => b.PlatformFeeTradesman);
		descriptor.Field(b => b.TotalEscrowAmount);
		descriptor.Field(b => b.IsFunded).Type<NonNullType<BooleanType>>();

		descriptor.Field(b => b.MilestonePayments)
			.Type<NonNullType<ListType<NonNullType<MilestonePaymentType>>>>()
			.ResolveWith<BuildSmart.Api.GraphQL.Resolvers.BookingResolvers>(r => r.GetMilestonePaymentsAsync(default!, default!, default!));

		// Relationships will be configured here later
	}
}