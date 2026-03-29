using BuildSmart.Api.DTOs;
using HotChocolate.Types;

namespace BuildSmart.Api.GraphQL.Types.Input;

public class SubmitBidInputType : InputObjectType<SubmitBidInput>
{
    protected override void Configure(IInputObjectTypeDescriptor<SubmitBidInput> descriptor)
    {
        descriptor.Description("Represents the input for submitting a bid against standardized job tasks.");

        descriptor.Field(t => t.TradesmanProfileId).Type<NonNullType<UuidType>>();
        descriptor.Field(t => t.JobPostId).Type<NonNullType<UuidType>>();
        descriptor.Field(t => t.Currency).Type<NonNullType<StringType>>();
        descriptor.Field(t => t.Comment).Type<StringType>();
        descriptor.Field(t => t.EarliestStartDate).Type<DateTimeType>();
        descriptor.Field(t => t.LatestStartDate).Type<DateTimeType>();
        descriptor.Field(t => t.EstimatedDurationDays).Type<IntType>();
        
        descriptor.Field(t => t.BidItems)
            .Type<NonNullType<ListType<NonNullType<BidItemInputType>>>>()
            .Description("The list of prices mapped to specific job tasks.");
    }
}

public class BidItemInputType : InputObjectType<BidItemInput>
{
    protected override void Configure(IInputObjectTypeDescriptor<BidItemInput> descriptor)
    {
        descriptor.Description("Represents a priced item mapping to a JobTask.");

        descriptor.Field(t => t.JobTaskId).Type<NonNullType<UuidType>>();
        descriptor.Field(t => t.PriceSubtotal).Type<NonNullType<DecimalType>>();
        descriptor.Field(t => t.Comment).Type<StringType>();
    }
}