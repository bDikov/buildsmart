using Xunit;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Api.GraphQL;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using BuildSmart.Infrastructure.Persistence;

namespace BuildSmart.Api.Tests;

public class RestoreAuctionTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;

    public RestoreAuctionTests(TestApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RestoreAuction_Logic_DeletesRecord()
    {
        // Using a fresh scope to get services
        using var scope = _factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 1. Setup Data directly in DB
        var user = new User { Id = Guid.NewGuid(), Email = "test-restore@bs.com", FirstName = "Test", LastName = "User", HashedPassword = "..." };
        var homeownerProfile = new HomeownerProfile { Id = Guid.NewGuid(), UserId = user.Id };
        var tradesmanProfile = new TradesmanProfile { Id = Guid.NewGuid(), UserId = user.Id };
        var category = new ServiceCategory { Id = Guid.NewGuid(), Name = "Test", TemplateStructure = "{}" };
        var project = new Project { Id = Guid.NewGuid(), Title = "Test", Description = "...", Homeowner = user };
        var job = new JobPost 
        { 
            Id = Guid.NewGuid(), 
            Title = "Test Job", 
            ServiceCategory = category, 
            Project = project, 
            Description = "...", 
            JobDetails = "{}", 
            Location = "Test",
            HomeownerProfileId = homeownerProfile.Id
        };
        
        var passedAction = new TradesmanAuctionAction 
        { 
            TradesmanProfileId = tradesmanProfile.Id, 
            JobPostId = job.Id, 
            ActionType = AuctionActionType.Passed 
        };

        context.Users.Add(user);
        context.HomeownerProfiles.Add(homeownerProfile);
        context.TradesmanProfiles.Add(tradesmanProfile);
        context.ServiceCategories.Add(category);
        context.Projects.Add(project);
        context.JobPosts.Add(job);
        context.TradesmanAuctionActions.Add(passedAction);
        await context.SaveChangesAsync();

        // 2. Prepare Mutation call
        var mutation = new Mutation();
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[] 
        { 
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, "Tradesman")
        }));

        // 3. Act
        var result = await mutation.RestoreAuction(job.Id, claimsPrincipal, unitOfWork);

        // 4. Assert
        Assert.True(result);
        
        // Verify record is gone from DB
        var exists = await context.TradesmanAuctionActions.AnyAsync(a => a.JobPostId == job.Id && a.TradesmanProfileId == tradesmanProfile.Id);
        Assert.False(exists);

        // Cleanup
        context.TradesmanAuctionActions.RemoveRange(context.TradesmanAuctionActions.Where(a => a.TradesmanProfileId == tradesmanProfile.Id));
        context.JobPosts.Remove(job);
        context.Projects.Remove(project);
        context.ServiceCategories.Remove(category);
        context.TradesmanProfiles.Remove(tradesmanProfile);
        context.HomeownerProfiles.Remove(homeownerProfile);
        context.Users.Remove(user);
        await context.SaveChangesAsync();
    }
}
