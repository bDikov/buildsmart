using BuildSmart.E2E.Tests.Infrastructure;
using BuildSmart.E2E.Tests.Pages;
using Microsoft.Playwright;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using BuildSmart.Infrastructure.Persistence;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;

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
        
        // Clear seeded global categories to avoid them injecting a General Questions step that blocks our tests
        await dbContext.ServiceCategories.Where(c => c.IsGlobal).ExecuteDeleteAsync();
        await dbContext.ServiceCategories.Where(c => c.Name == "Project Details").ExecuteDeleteAsync();
        
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

        var projectDetailsCategory = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            Name = "Project Details",
            Status = CategoryStatus.Active,
            TemplateStructure = @"{
                ""isProjectDetails"": true,
                ""questions"": [
                    { ""id"": ""proj_location"", ""text"": ""Where is the location?"", ""type"": ""text"", ""required"": true },
                    { ""id"": ""proj_start_timeline"", ""text"": ""When do you hope to start?"", ""type"": ""text"", ""required"": true }
                ]
            }"
        };
        
        dbContext.Users.Add(testUser);
        dbContext.ServiceCategories.Add(testCategory);
        dbContext.ServiceCategories.Add(projectDetailsCategory);
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

        // 3. Step 0: Category Selection
        var wizardPage = new JobWizardPage(Page);
        await wizardPage.SelectCategoryAsync(categoryName);
        await wizardPage.ClickNextAsync();

        // 4. Step 1: Category Questions
        await wizardPage.ExpectQuestionVisibleAsync("How many sockets?");
        await wizardPage.FillNumberInputAsync("How many sockets?", "10");
        await wizardPage.ClickNextAsync();

        // 5. Step 2: Review & Submit (Pre-final step)
        await wizardPage.ClickNextAsync();

        // 6. Step 3: Project Details (Final step, post-submission)
        await wizardPage.ExpectQuestionVisibleAsync("Where is the location?");
        await wizardPage.FillTextInputAsync("Where is the location?", "Sofia, Bulgaria");
        await wizardPage.FillTextInputAsync("When do you hope to start?", "In 2 weeks");
        await wizardPage.ClickNextAsync();

        // 7. Assert: Verify we reached the Offer Building success page
        var successMessage = Page.Locator("h3:has-text('preparing your offer')");
        await Expect(successMessage).ToBeVisibleAsync();
    }

    [Test]
    public async Task Homeowner_JobWizard_SubsequentialQuestions_HideAndShowCorrectly()
    {
        // 0. SEED DATA
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Clear seeded global categories to avoid them injecting a General Questions step that blocks our tests
        await dbContext.ServiceCategories.Where(c => c.IsGlobal).ExecuteDeleteAsync();
        await dbContext.ServiceCategories.Where(c => c.Name == "Project Details").ExecuteDeleteAsync();
        
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
            IsGlobal = false, // MUST be false to appear on category selection page
            Status = CategoryStatus.Active,
            TemplateStructure = @"{
                ""questions"": [
                    { ""id"": ""q1"", ""text"": ""Main Question"", ""type"": ""multiselect"", ""required"": true, ""options"": [""Option A"", ""Option B""] },
                    { ""id"": ""q2"", ""text"": ""Sub Question A"", ""type"": ""multiselect"", ""required"": true, ""options"": [""Sub A1"", ""Sub A2""], ""dependsOn"": ""q1"", ""dependsOnValue"": ""Option A"" },
                    { ""id"": ""q3"", ""text"": ""Deep Question A1"", ""type"": ""number"", ""required"": true, ""dependsOn"": ""q2"", ""dependsOnValue"": ""Sub A1"" }
                ]
            }"
        };

        var projectDetailsCategory = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            Name = "Project Details",
            Status = CategoryStatus.Active,
            TemplateStructure = @"{
                ""isProjectDetails"": true,
                ""questions"": []
            }"
        };
        
        dbContext.Users.Add(testUser);
        dbContext.ServiceCategories.Add(subSeqCategory);
        dbContext.ServiceCategories.Add(projectDetailsCategory);
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

        // 3. Step 0: Category Selection
        var wizardPage = new JobWizardPage(Page);
        await wizardPage.SelectCategoryAsync(categoryName);
        await wizardPage.ClickNextAsync();

        // 4. We are on Questions step
        // Assert base state: Main Question is visible, Sub Question A and Deep Question A1 are hidden
        await wizardPage.ExpectQuestionVisibleAsync("Main Question");
        await wizardPage.ExpectQuestionHiddenAsync("Sub Question A");
        await wizardPage.ExpectQuestionHiddenAsync("Deep Question A1");

        // 5. Act: Select 'Option A' on Main Question
        await wizardPage.SelectChoiceOptionAsync("Main Question", "Option A");
        
        // Assert: Sub Question A should appear now
        await wizardPage.ExpectQuestionVisibleAsync("Sub Question A");
        await wizardPage.ExpectQuestionHiddenAsync("Deep Question A1"); // Deep question still hidden

        // 6. Act: Select 'Sub A1' on Sub Question A
        await wizardPage.SelectChoiceOptionAsync("Sub Question A", "Sub A1");

        // Assert: Deep Question A1 should appear now
        await wizardPage.ExpectQuestionVisibleAsync("Deep Question A1");

        // 7. Act: Deselect 'Option A' on Main Question (click it again)
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
        
        // Clear seeded global categories to avoid them injecting a General Questions step that blocks our tests
        await dbContext.ServiceCategories.Where(c => c.IsGlobal).ExecuteDeleteAsync();
        await dbContext.ServiceCategories.Where(c => c.Name == "Project Details").ExecuteDeleteAsync();
        
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
        
        var categoryName = $"Localized Category-{uniqueLangId}";
        var localizedCategory = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            Name = categoryName,
            IsGlobal = false, // Isolated to this test
            Status = CategoryStatus.Active,
            TemplateStructure = @"{
              ""questions"": [
                { ""id"": ""global_property_type"", ""text"": { ""bg"": ""Какъв е типът на имота?"", ""en"": ""What is the property type?"" }, ""type"": ""choice"", ""required"": true, ""options"": { ""bg"": [""Апартамент""], ""en"": [""Apartment""] } }
              ]
            }"
        };

        var projectDetailsCategory = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            Name = "Project Details",
            Status = CategoryStatus.Active,
            TemplateStructure = @"{
                ""isProjectDetails"": true,
                ""questions"": []
            }"
        };
        
        dbContext.Users.Add(testUser);
        dbContext.ServiceCategories.Add(localizedCategory);
        dbContext.ServiceCategories.Add(projectDetailsCategory);
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

        // 4. Step 0: Category Selection
        var wizardPage = new JobWizardPage(Page);
        await wizardPage.SelectCategoryAsync(categoryName);
        await wizardPage.ClickNextAsync();

        // 5. Assert: We should see the ENGLISH text of the question
        await wizardPage.ExpectQuestionVisibleAsync("What is the property type?");
    }

    [Test]
    public async Task Homeowner_JobWizard_ElectricalCategory_CompletesSuccessfully()
    {
        // 0. SEED DATA
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Clear seeded global categories to isolate the test
        await dbContext.ServiceCategories.Where(c => c.IsGlobal).ExecuteDeleteAsync();
        await dbContext.ServiceCategories.Where(c => c.Name == "Project Details").ExecuteDeleteAsync();
        
        var uniqueElecId = Guid.NewGuid().ToString().Substring(0, 8);
        var testUser = new User 
        { 
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "UserElec",
            Email = $"testuserelec{uniqueElecId}@buildsmart.com", 
            HashedPassword = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRoleTypes.Homeowner,
            PreferredLanguage = "bg",
            HomeownerProfile = new HomeownerProfile()
        };
        
        // Seed the exact UX template we designed for Electrical
        var categoryName = $"Електрическа Инсталация-{uniqueElecId}";
        var elecCategory = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            Name = categoryName,
            Status = CategoryStatus.Active,
            TemplateStructure = @"{
                ""questions"": [
                    { ""id"": ""elec_scope"", ""text"": ""Какъв е мащабът на ремонта?"", ""type"": ""choice"", ""required"": true, ""options"": [""Цялостна подмяна"", ""Частичен ремонт""] },
                    { ""id"": ""elec_heavy_appliances"", ""text"": ""Кои мощни уреди ще имате?"", ""type"": ""multiselect"", ""required"": true, ""options"": [""Фурна"", ""Индукционен котлон"", ""Пералня""] },
                    { ""id"": ""elec_outlets_comfort"", ""text"": ""Колко контакти желаете във всяка стая?"", ""type"": ""choice"", ""required"": true, ""options"": [""Базово"", ""Комфорт""] }
                ]
            }"
        };

        var projectDetailsCategory = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            Name = "Project Details",
            Status = CategoryStatus.Active,
            TemplateStructure = @"{
                ""isProjectDetails"": true,
                ""questions"": [
                    { ""id"": ""proj_location"", ""text"": ""Къде е обектът?"", ""type"": ""text"", ""required"": true },
                    { ""id"": ""proj_start_timeline"", ""text"": ""Кога искате да започнете?"", ""type"": ""text"", ""required"": true }
                ]
            }"
        };
        
        dbContext.Users.Add(testUser);
        dbContext.ServiceCategories.Add(elecCategory);
        dbContext.ServiceCategories.Add(projectDetailsCategory);
        await dbContext.SaveChangesAsync();

        // 1. Arrange - Inject the Language Header and Cookie
        await Context.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
        {
            { "Accept-Language", "bg-BG,bg;q=0.9" }
        });
        
        await Context.AddCookiesAsync(new[]
        {
            new Microsoft.Playwright.Cookie
            {
                Name = ".AspNetCore.Culture",
                Value = "c=bg|uic=bg",
                Url = BaseUrl
            }
        });

        // 2. Navigate & Login
        var loginPage = new LoginPage(Page);
        await loginPage.GotoAsync(BaseUrl);
        await loginPage.LoginWithCredentialsAsync($"testuserelec{uniqueElecId}@buildsmart.com", "Password123!");
        await Expect(Page).Not.ToHaveURLAsync(new Regex(".*login"), new() { Timeout = 10000 });
        
        await Page.WaitForTimeoutAsync(1000);

        // 2. Start Project Wizard
        var myProjectsPage = new MyProjectsPage(Page);
        await myProjectsPage.GotoAsync(BaseUrl);
        await myProjectsPage.ClickCreateNewProjectAsync();

        // 3. Step 0: Select Category
        var wizardPage = new JobWizardPage(Page);
        await wizardPage.SelectCategoryAsync(categoryName);
        await wizardPage.ClickNextAsync();

        // 4. Step 1: Answer Questions (Testing the new Choice/Multiselect cards)
        await wizardPage.ExpectQuestionVisibleAsync("Какъв е мащабът на ремонта?");
        
        // Select Scope
        await wizardPage.SelectChoiceOptionAsync("Какъв е мащабът на ремонта?", "Цялостна подмяна");
        
        // Select Appliances (Multiselect)
        await wizardPage.SelectChoiceOptionAsync("Кои мощни уреди ще имате?", "Фурна");
        await wizardPage.SelectChoiceOptionAsync("Кои мощни уреди ще имате?", "Пералня");

        // Select Comfort Level
        await wizardPage.SelectChoiceOptionAsync("Колко контакти желаете във всяка стая?", "Комфорт");

        // Click next to go to Review
        await wizardPage.ClickNextAsync();
        
        // 5. Step 2: Review (Pre-final step)
        await wizardPage.ClickNextAsync();

        // 6. Step 3: Project Details (Final step, post-submission)
        await wizardPage.ExpectQuestionVisibleAsync("Къде е обектът?");
        await wizardPage.FillTextInputAsync("Къде е обектът?", "София");
        await wizardPage.FillTextInputAsync("Кога искате да започнете?", "След месец");
        await wizardPage.ClickNextAsync(); // Save & View Offer
        
        // 7. Verify we reached the success screen
        var successMessage = Page.Locator("h3:has-text('Подготвяме вашата оферта')");
        await Expect(successMessage).ToBeVisibleAsync();
        
        var validationErrors = Page.Locator(".text-danger"); // Standard bootstrap validation
        await Expect(validationErrors).ToHaveCountAsync(0);
    }

    [Test]
    public async Task GuestUser_JobWizard_ProceedsWithoutAuth_AndShowsUserCategoryStep()
    {
        // 0. SEED DATA
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Clear seeded global categories to avoid interfering
        await dbContext.ServiceCategories.Where(c => c.IsGlobal).ExecuteDeleteAsync();
        await dbContext.ServiceCategories.Where(c => c.Name == "Project Details").ExecuteDeleteAsync();
        await dbContext.ServiceCategories.Where(c => c.Name == "User Information").ExecuteDeleteAsync();

        var uniqueId = Guid.NewGuid().ToString().Substring(0, 8);
        var categoryName = $"Electrical-{uniqueId}";

        var testCategory = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            Name = categoryName,
            Status = CategoryStatus.Active,
            Type = CategoryType.CategorySpecific,
            TemplateStructure = "{\"questions\": [{\"id\":\"q1\",\"text\":\"How many sockets?\",\"type\":\"number\"}]}"
        };

        var userCategory = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            Name = "User Information",
            Status = CategoryStatus.Active,
            Type = CategoryType.UserType,
            TemplateStructure = @"{
                ""questions"": [
                    { ""id"": ""user_name"", ""text"": ""Full Name"", ""type"": ""text"", ""required"": true },
                    { ""id"": ""user_email"", ""text"": ""Email Address"", ""type"": ""text"", ""required"": true },
                    { ""id"": ""user_phone"", ""text"": ""Phone Number"", ""type"": ""text"", ""required"": true }
                ]
            }"
        };

        dbContext.ServiceCategories.Add(testCategory);
        dbContext.ServiceCategories.Add(userCategory);
        await dbContext.SaveChangesAsync();

        // 1. Act - Navigate to Job Wizard directly without logging in (guest)
        await Page.GotoAsync(BaseUrl + "/job-wizard");
        Console.WriteLine($"[Test] Immediate URL: {Page.Url}");
        await Task.Delay(3000);
        Console.WriteLine($"[Test] URL after 3s delay: {Page.Url}");

        // 2. Step 0: Category Selection
        var wizardPage = new JobWizardPage(Page);
        await wizardPage.SelectCategoryAsync(categoryName);
        await wizardPage.ClickNextAsync();

        // 3. Step 1: User Information (Should be injected since we are unauthenticated)
        await wizardPage.ExpectQuestionVisibleAsync("Full Name");
        await wizardPage.ExpectQuestionVisibleAsync("Email Address");
        await wizardPage.ExpectQuestionVisibleAsync("Phone Number");

        // Fill in guest details
        await wizardPage.FillTextInputAsync("Full Name", "Guest User");
        await wizardPage.FillTextInputAsync("Email Address", $"guest{uniqueId}@example.com");
        await wizardPage.FillTextInputAsync("Phone Number", "0888123456");
        await wizardPage.ClickNextAsync();

        // 4. Step 2: Specific Category Questions
        await wizardPage.ExpectQuestionVisibleAsync("How many sockets?");
        await wizardPage.FillNumberInputAsync("How many sockets?", "5");
        await wizardPage.ClickNextAsync();

        // 5. Step 3: Review & Submit (Pre-final step)
        await wizardPage.ClickNextAsync();
    }
}
