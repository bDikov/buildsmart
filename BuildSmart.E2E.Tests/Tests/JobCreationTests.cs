using BuildSmart.E2E.Tests.Infrastructure;
using BuildSmart.E2E.Tests.Pages;
using Microsoft.Playwright;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using BuildSmart.Infrastructure.Persistence;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;

namespace BuildSmart.E2E.Tests.Tests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class JobCreationTests : TestBase
{
    [Test]
    public async Task Homeowner_CompleteJobWizard_SuccessfullyCreatesProject()
    {
        // 0. SEED DATA
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var testUser = new User 
        { 
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "User",
            Email = "testuser@buildsmart.com", 
            HashedPassword = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRoleTypes.Homeowner,
            PreferredLanguage = "en",
            HomeownerProfile = new HomeownerProfile()
        };
        
        var testCategory = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            Name = "Electrical",
            Status = CategoryStatus.Active,
            TemplateStructure = "{\"questions\": [{\"id\":\"q1\",\"text\":\"How many sockets?\",\"type\":\"number\"}]}"
        };
        
        dbContext.Users.Add(testUser);
        dbContext.ServiceCategories.Add(testCategory);
        await dbContext.SaveChangesAsync();

        // 1. Arrange - Navigate to Login
        var loginPage = new LoginPage(Page);
        await loginPage.GotoAsync(BaseUrl);
        await loginPage.LoginWithCredentialsAsync("testuser@buildsmart.com", "Password123!");

        // Wait for the login redirect to complete successfully
        await Expect(Page).Not.ToHaveURLAsync(new Regex(".*login"), new() { Timeout = 10000 });

        // 2. Navigate to Projects and Create New
        var myProjectsPage = new MyProjectsPage(Page);
        await myProjectsPage.GotoAsync(BaseUrl);
        await myProjectsPage.ClickCreateNewProjectAsync();

        // Expectation: URL should navigate to wizard
        await Expect(Page).ToHaveURLAsync(new Regex(".*job-wizard"));

        // 3. Act - Fill out Step 0
        var wizardPage = new JobWizardPage(Page);
        string uniqueTitle = $"E2E Test Project {Guid.NewGuid().ToString().Substring(0, 5)}";
        await wizardPage.FillBasicInfoAsync(
            title: uniqueTitle,
            location: "Sofia, Bulgaria",
            description: "Full apartment renovation. Needed ASAP."
        );
        
        await wizardPage.ClickNextAsync();

        // 4. Act - Select Category and proceed
        await wizardPage.SelectCategoryAsync("Electrical");
        await wizardPage.ClickNextAsync();

        // 5. Assert we reached the next step (questions or success)
        await Expect(Page).Not.ToHaveURLAsync(new Regex(".*login"));
    }
}
