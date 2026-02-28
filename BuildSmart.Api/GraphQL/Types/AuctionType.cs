using BuildSmart.Api.GraphQL.Types;

namespace BuildSmart.Api.GraphQL.Types;

public class AuctionType : ObjectType<Auction>
{
    protected override void Configure(IObjectTypeDescriptor<Auction> descriptor)
    {
        descriptor.Description("Represents a wrapper for a job post during its active bidding phase (Auction).");

        descriptor.Field(a => a.Job)
            .Type<JobPostType>()
            .Description("The underlying job post information.");

        descriptor.Field(a => a.Bids)
            .Description("The current list of bids for this job.");

        descriptor.Field(a => a.Questions)
            .Description("The Q&A history for this auction.");
    }
}
