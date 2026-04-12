using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Api.GraphQL.Types;

public class MilestonePaymentType : ObjectType<MilestonePayment>
{
    protected override void Configure(IObjectTypeDescriptor<MilestonePayment> descriptor)
    {
        descriptor.Description("Represents a specific task payout milestone linked to a booking.");

        descriptor.Field(mp => mp.Id).Type<NonNullType<IdType>>();
        descriptor.Field(mp => mp.BookingId).Type<NonNullType<IdType>>();
        descriptor.Field(mp => mp.JobTaskId).Type<NonNullType<IdType>>();
        descriptor.Field(mp => mp.AmountAllocated);
        descriptor.Field(mp => mp.Status).Type<NonNullType<EnumType<BuildSmart.Core.Domain.Enums.MilestoneStatus>>>();
        descriptor.Field(mp => mp.StripeTransferId).Type<StringType>();
        descriptor.Field(mp => mp.CreatedAt).Type<NonNullType<DateTimeType>>();
        descriptor.Field(mp => mp.UpdatedAt).Type<NonNullType<DateTimeType>>();
    }
}
