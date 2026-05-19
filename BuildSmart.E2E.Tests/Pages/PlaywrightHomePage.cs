using Microsoft.Playwright;

namespace BuildSmart.E2E.Tests.Pages;

public class PlaywrightHomePage : BasePage
{
    // Locators are strictly private. We do not expose Playwright implementation details to tests.
    private ILocator GetStartedButton => _page.Locator("text=Get Started");

    public PlaywrightHomePage(IPage page) : base(page)
    {
    }

    public async Task GotoAsync(string baseUrl)
    {
        await _page.GotoAsync(baseUrl);
    }

    public async Task ClickGetStartedAsync()
    {
        // Relying on auto-waiting
        await GetStartedButton.ClickAsync();
    }

    // Since Expectations are meant to be performed in the Test layer,
    // we can return a Locator if we specifically need to assert its state.
    public ILocator GetStartedLinkLocator()
    {
        return GetStartedButton;
    }
}
