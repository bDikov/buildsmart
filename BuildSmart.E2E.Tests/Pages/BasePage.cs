using Microsoft.Playwright;

namespace BuildSmart.E2E.Tests.Pages;

public abstract class BasePage
{
    protected readonly IPage _page;

    public BasePage(IPage page)
    {
        _page = page;
    }

    // Common UI interaction methods for the entire application will go here
}
