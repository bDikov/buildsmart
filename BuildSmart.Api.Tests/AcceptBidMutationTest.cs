using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;

namespace BuildSmart.Api.Tests
{
	public class AcceptBidMutationTest : IClassFixture<TestApplicationFactory>
	{
		private readonly TestApplicationFactory _factory;

		public AcceptBidMutationTest(TestApplicationFactory factory)
		{
			_factory = factory;
		}

		[Fact]
		public async Task TestAcceptBidMutation()
		{
			var executor = await _factory.Services.GetRequiredService<IRequestExecutorResolver>().GetRequestExecutorAsync();

			// Note: We are using a fake ID here just to see if the pipeline crashes before hitting the DB
			// or if it crashes inside the PaymentService with a specific EF Core error.
			var mutation = @"
			mutation {
			  acceptBid(bidId: ""00000000-0000-0000-0000-000000000000"") {
			    id
			    status
			    agreedBidAmount {
			      total
			      currency
			    }
			    platformFeeHomeowner {
			      total
			      currency
			    }
			    totalEscrowAmount {
			      total
			      currency
			    }
			    milestonePayments {
			      id
			      jobTaskId
			      amountAllocated {
			        total
			        currency
			      }
			      status
			    }
			  }
			}";
			var result = await executor.ExecuteAsync(mutation);
			var json = result.ToString();

			Assert.DoesNotContain("Unexpected Execution Error", json);
		}
	}
}