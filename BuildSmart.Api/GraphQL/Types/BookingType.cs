using BuildSmart.Core.Domain.Entities;
using HotChocolate.Types;

namespace BuildSmart.Api.GraphQL.Types;

public class BookingType : ObjectType<Booking>
{
    protected override void Configure(IObjectTypeDescriptor<Booking> descriptor)
    {
        descriptor.Description("Represents an active escrow contract between a Homeowner and a Tradesman.");

        // Manually resolve complex properties to prevent EF Core 8 ComplexProperty null check translation crashes
        descriptor.Field(b => b.AgreedBidAmount)
            .Resolve(ctx => ctx.Parent<Booking>().AgreedBidAmount);
            
        descriptor.Field(b => b.PlatformFeeHomeowner)
            .Resolve(ctx => ctx.Parent<Booking>().PlatformFeeHomeowner);
            
        descriptor.Field(b => b.PlatformFeeTradesman)
            .Resolve(ctx => ctx.Parent<Booking>().PlatformFeeTradesman);
            
        descriptor.Field(b => b.TotalEscrowAmount)
            .Resolve(ctx => ctx.Parent<Booking>().TotalEscrowAmount);
    }
}
