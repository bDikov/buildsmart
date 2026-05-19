using Microsoft.Playwright;

namespace BuildSmart.E2E.Tests.Pages;

public class MyProjectsPage : BasePage
{
    private ILocator CreateNewButton => _page.Locator(".header-section .bs-btn-primary");
    private ILocator LoadingState => _page.Locator(".loading-state");
    private ILocator ProjectCards => _page.Locator(".project-card");

    public MyProjectsPage(IPage page) : base(page)
    {
    }

    public async Task GotoAsync(string baseUrl)
    {
        await _page.GotoAsync($"{baseUrl}/my-projects");
    }

    public async Task WaitForLoadingAsync()
    {
        // Wait for the loading spinner to disappear
        if (await LoadingState.IsVisibleAsync())
        {
            await LoadingState.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
        }
    }

    public async Task ClickCreateNewProjectAsync()
    {
        await WaitForLoadingAsync();
        await CreateNewButton.ClickAsync();
    }

    public async Task<int> GetProjectCountAsync()
    {
        await WaitForLoadingAsync();
        return await ProjectCards.CountAsync();
    }
}
