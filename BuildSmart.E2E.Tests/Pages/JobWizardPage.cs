using Microsoft.Playwright;
using System.Threading.Tasks;

namespace BuildSmart.E2E.Tests.Pages;

public class JobWizardPage : BasePage
{
    // Step 0: Basic Info Locators
    // The inputs are inside bs-card, order: Title, Location, Description
    private ILocator TitleInput => _page.Locator(".bs-card input[type='text']").First;
    private ILocator LocationInput => _page.Locator(".bs-card input[type='text']").Nth(1);
    private ILocator DescriptionInput => _page.Locator(".bs-card textarea").First;
    private ILocator NextButton => _page.Locator("button.bs-btn-primary").First;

    // Step 1: Category Selection
    private ILocator CategoryCards => _page.Locator(".category-card"); 

    // Wizard navigation generic
    private ILocator LoadingSpinner => _page.Locator(".spinner-border");

    public JobWizardPage(IPage page) : base(page)
    {
    }

    public async Task WaitForLoadingAsync()
    {
        if (await LoadingSpinner.IsVisibleAsync())
        {
            await LoadingSpinner.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
        }
    }

    public async Task FillBasicInfoAsync(string title, string location, string description)
    {
        await WaitForLoadingAsync();
        
        // Ensure inputs are visible before acting
        await TitleInput.WaitForAsync();
        
        await TitleInput.FillAsync(title);
        await LocationInput.FillAsync(location);
        await DescriptionInput.FillAsync(description);
    }

    public async Task ClickNextAsync()
    {
        await NextButton.ClickAsync();
        await WaitForLoadingAsync();
    }

    public async Task SelectCategoryAsync(string categoryName)
    {
        // Give category list a moment to render
        await _page.Locator(".category-list").WaitForAsync();

        var label = _page.Locator($".category-item:has-text('{categoryName}')");
        await label.ClickAsync(); // Click the label to trigger the native checkbox change event

        // Wait for Blazor binding to sync before clicking Next
        await _page.WaitForTimeoutAsync(500);
    }
    // --- Question Step Helpers ---
    public async Task ExpectQuestionVisibleAsync(string partialQuestionText)
    {
        var locator = _page.Locator($"label.form-label:has-text('{partialQuestionText}')");
        await Microsoft.Playwright.Assertions.Expect(locator).ToBeVisibleAsync();
    }

    public async Task ExpectQuestionHiddenAsync(string partialQuestionText)
    {
        var locator = _page.Locator($"label.form-label:has-text('{partialQuestionText}')");
        await Microsoft.Playwright.Assertions.Expect(locator).ToBeHiddenAsync();
    }

    public async Task SelectChoiceOptionAsync(string partialQuestionText, string optionText)
    {
        var questionWrapper = _page.Locator($".mb-4:has(label.form-label:has-text('{partialQuestionText}'))");
        await questionWrapper.Locator($"label.form-check-label:has-text('{optionText}')").ClickAsync();
        await _page.WaitForTimeoutAsync(500); // Wait for Blazor binding to evaluate subsequential questions
    }}