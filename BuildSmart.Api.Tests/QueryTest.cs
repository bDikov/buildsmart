using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;

namespace BuildSmart.Api.Tests
{
    public class QueryTest : IClassFixture<TestApplicationFactory>
    {
        private readonly TestApplicationFactory _factory;

        public QueryTest(TestApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task TestGetBidDetails()
        {
            var executor = await _factory.Services.GetRequiredService<IRequestExecutorResolver>().GetRequestExecutorAsync();
            var query = @"
            query {
              allJobPosts {
                bids {
                  id
                  amount {
                    total
                    currency
                  }
                  bidItems {
                    id
                    price {
                      total
                      currency
                    }
                  }
                }
              }
            }";
            var result = await executor.ExecuteAsync(query);
            var json = result.ToString();
            Assert.DoesNotContain("Unexpected Execution Error", json);
            Assert.DoesNotContain("Comparing complex types to null is not supported", json);
        }
    }
}
