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
        
        var uniqueUserGuid = Guid.NewGuid().ToString().Substring(0, 8);
        var testUser = new User 
        { 
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "User",
            Email = $"testuser{uniqueUserGuid}@buildsmart.com", 
            HashedPassword = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRoleTypes.Homeowner,
            PreferredLanguage = "en",
            HomeownerProfile = new HomeownerProfile()
        };
        
        var categoryName = $"Electrical-{uniqueUserGuid}";
        var testCategory = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            Name = categoryName,
            Status = CategoryStatus.Active,
            TemplateStructure = "{\"questions\": [{\"id\":\"q1\",\"text\":\"How many sockets?\",\"type\":\"number\"}]}"
        };
        
        dbContext.Users.Add(testUser);
        dbContext.ServiceCategories.Add(testCategory);
        await dbContext.SaveChangesAsync();

        // 1. Arrange - Navigate to Login
        var loginPage = new LoginPage(Page);
        await loginPage.GotoAsync(BaseUrl);
        await loginPage.LoginWithCredentialsAsync($"testuser{uniqueUserGuid}@buildsmart.com", "Password123!");

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
        await wizardPage.SelectCategoryAsync(categoryName);
        await wizardPage.ClickNextAsync();

        // 5. Assert we reached the next step (questions or success)
        await Expect(Page).Not.ToHaveURLAsync(new Regex(".*login"));
    }

    [Test]
    public async Task Homeowner_JobWizard_SubsequentialQuestions_HideAndShowCorrectly()
    {
        // 0. SEED DATA
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var uniqueSubSeqId = Guid.NewGuid().ToString().Substring(0, 8);
        var testUser = new User 
        { 
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "UserQ",
            Email = $"testuserq{uniqueSubSeqId}@buildsmart.com", 
            HashedPassword = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRoleTypes.Homeowner,
            PreferredLanguage = "en",
            HomeownerProfile = new HomeownerProfile()
        };
        
        // Define a category specifically for testing the sub-sequential logic
        var categoryName = $"Tiling SubSeq Test-{uniqueSubSeqId}";
        var subSeqCategory = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            Name = categoryName,
            IsGlobal = false,
            Status = CategoryStatus.Active,
            TemplateStructure = @"{
                ""questions"": [
                    { ""id"": ""q1"", ""text"": ""Main Question"", ""type"": ""multiselect"", ""required"": true, ""options"": [""Option A"", ""Option B""] },
                    { ""id"": ""q2"", ""text"": ""Sub Question A"", ""type"": ""multiselect"", ""required"": true, ""options"": [""Sub A1"", ""Sub A2""], ""dependsOn"": ""q1"", ""dependsOnValue"": ""Option A"" },
                    { ""id"": ""q3"", ""text"": ""Deep Question A1"", ""type"": ""number"", ""required"": true, ""dependsOn"": ""q2"", ""dependsOnValue"": ""Sub A1"" }
                ]
            }"
        };

        var dummyCategoryName = $"Dummy Category Seq-{uniqueSubSeqId}";
        var dummyCategory = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            Name = dummyCategoryName,
            IsGlobal = false,
            Status = CategoryStatus.Active,
            TemplateStructure = "{}"
        };
        
        dbContext.Users.Add(testUser);
        dbContext.ServiceCategories.Add(subSeqCategory);
        dbContext.ServiceCategories.Add(dummyCategory);
        await dbContext.SaveChangesAsync();

        // 1. Navigate & Login
        var loginPage = new LoginPage(Page);
        await loginPage.GotoAsync(BaseUrl);
        await loginPage.LoginWithCredentialsAsync($"testuserq{uniqueSubSeqId}@buildsmart.com", "Password123!");
        await Expect(Page).Not.ToHaveURLAsync(new Regex(".*login"), new() { Timeout = 10000 });
        
        // Wait for Blazor MainLayout to fetch user profile and apply language cookie / reload if necessary
        await Page.WaitForTimeoutAsync(2000);

        // 2. Start Project Wizard
        var myProjectsPage = new MyProjectsPage(Page);
        await myProjectsPage.GotoAsync(BaseUrl);
        await myProjectsPage.ClickCreateNewProjectAsync();

        // 3. Fill Basic Info
        var wizardPage = new JobWizardPage(Page);
        await wizardPage.FillBasicInfoAsync("Sub-sequential Flow Test", "Test City", "Testing UI toggles");
        await wizardPage.ClickNextAsync();

        // 4. Select Category
        await wizardPage.SelectCategoryAsync(categoryName);
        await wizardPage.ClickNextAsync();

        // 5. We are on Questions step
        // Assert base state: Main Question is visible, Sub Question A and Deep Question A1 are hidden
        await wizardPage.ExpectQuestionVisibleAsync("Main Question");
        await wizardPage.ExpectQuestionHiddenAsync("Sub Question A");
        await wizardPage.ExpectQuestionHiddenAsync("Deep Question A1");

        // 6. Act: Select 'Option A' on Main Question
        await wizardPage.SelectChoiceOptionAsync("Main Question", "Option A");
        
        // Assert: Sub Question A should appear now
        await wizardPage.ExpectQuestionVisibleAsync("Sub Question A");
        await wizardPage.ExpectQuestionHiddenAsync("Deep Question A1"); // Deep question still hidden

        // 7. Act: Select 'Sub A1' on Sub Question A
        await wizardPage.SelectChoiceOptionAsync("Sub Question A", "Sub A1");

        // Assert: Deep Question A1 should appear now
        await wizardPage.ExpectQuestionVisibleAsync("Deep Question A1");

        // 8. Act: Deselect 'Option A' on Main Question (click it again)
        await wizardPage.SelectChoiceOptionAsync("Main Question", "Option A");

        // Assert: Both nested questions should immediately hide due to recursive logic
        await wizardPage.ExpectQuestionHiddenAsync("Sub Question A");
        await wizardPage.ExpectQuestionHiddenAsync("Deep Question A1");
    }

    [Test]
    public async Task Homeowner_JobWizard_EnglishLanguage_RendersCorrectly()
    {
        // 0. SEED DATA
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var uniqueLangId = Guid.NewGuid().ToString().Substring(0, 8);
        var testUser = new User 
        { 
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "UserLang",
            Email = $"testuserlang{uniqueLangId}@buildsmart.com", 
            HashedPassword = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRoleTypes.Homeowner,
            PreferredLanguage = "en", // Explicitly English
            HomeownerProfile = new HomeownerProfile()
        };
        
        var globalCategory = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            Name = $"Global Questions-{uniqueLangId}",
            IsGlobal = true, // Force it to appear for every job
            Status = CategoryStatus.Active,
            TemplateStructure = @"{
              ""questions"": [
                { ""id"": ""global_property_type"", ""text"": { ""bg"": ""Какъв е типът на имота?"", ""en"": ""What is the property type?"" }, ""type"": ""choice"", ""required"": true, ""options"": { ""bg"": [""Апартамент""], ""en"": [""Apartment""] } }
              ]
            }"
        };

        var dummyCategoryName = $"Dummy Category-{uniqueLangId}";
        var dummyCategory = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            Name = dummyCategoryName,
            IsGlobal = false,
            Status = CategoryStatus.Active,
            TemplateStructure = "{}"
        };
        
        dbContext.Users.Add(testUser);
        dbContext.ServiceCategories.Add(globalCategory);
        dbContext.ServiceCategories.Add(dummyCategory);
        await dbContext.SaveChangesAsync();

        // 1. Arrange - Inject the Language Header and Cookie
        await Context.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
        {
            { "Accept-Language", "en-US,en;q=0.9" }
        });
        
        await Context.AddCookiesAsync(new[]
        {
            new Microsoft.Playwright.Cookie
            {
                Name = ".AspNetCore.Culture",
                Value = "c=en|uic=en",
                Url = BaseUrl
            }
        });

        // 2. Navigate & Login
        var loginPage = new LoginPage(Page);
        await loginPage.GotoAsync(BaseUrl);
        await loginPage.LoginWithCredentialsAsync($"testuserlang{uniqueLangId}@buildsmart.com", "Password123!");
        await Expect(Page).Not.ToHaveURLAsync(new Regex(".*login"), new() { Timeout = 10000 });
        
        // Wait briefly for safety
        await Page.WaitForTimeoutAsync(1000);

        // 3. Start Project Wizard
        var myProjectsPage = new MyProjectsPage(Page);
        await myProjectsPage.GotoAsync(BaseUrl);
        await myProjectsPage.ClickCreateNewProjectAsync();

        // 3. Fill Basic Info
        var wizardPage = new JobWizardPage(Page);
        await wizardPage.FillBasicInfoAsync("Lang Test", "City", "Lang UI Test");
        await wizardPage.ClickNextAsync();

        // 4. Select Category
        await wizardPage.SelectCategoryAsync(dummyCategoryName);
        await wizardPage.ClickNextAsync();

        // 5. Assert: We should see the ENGLISH text of the global question
        await wizardPage.ExpectQuestionVisibleAsync("What is the property type?");
    }
}
