using BuildSmart.Core.Domain.Entities;
using HotChocolate.Types;

namespace BuildSmart.Api.GraphQL.Types;

public class BidType : ObjectType<Bid>
{
    protected override void Configure(IObjectTypeDescriptor<Bid> descriptor)
    {
        descriptor.Description("Represents a proposal submitted by a tradesman for a specific job.");

        // Manually resolve Amount to prevent EF Core 8 ComplexProperty null check translation crashes
        descriptor.Field(b => b.Amount)
            .Description("The overall proposed total amount for the job.")
            .Resolve(ctx => ctx.Parent<Bid>().Amount);
            
        // Explicitly declare other complex relationships if needed, or let HotChocolate infer them
    }
}
