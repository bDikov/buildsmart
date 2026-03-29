using BuildSmart.Api.GraphQL.Types;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Api.GraphQL.DataLoaders;

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
			.Description("The current list of bids for this job.")
			.Resolve(async context =>
			{
				var auction = context.Parent<Auction>();
				return await context.DataLoader<BidsByJobPostIdDataLoader>()
					.LoadAsync(auction.Job.Id, context.RequestAborted);
			});

		descriptor.Field(a => a.Questions)
			.Description("The Q&A history for this auction.")
			.Resolve(async context =>
			{
				var auction = context.Parent<Auction>();
				return await context.DataLoader<QuestionsByJobPostIdDataLoader>()
					.LoadAsync(auction.Job.Id, context.RequestAborted);
			});
	}
}