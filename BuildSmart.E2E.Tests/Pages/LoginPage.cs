using Microsoft.Playwright;

namespace BuildSmart.E2E.Tests.Pages;

public class LoginPage : BasePage
{
    private ILocator EmailInput => _page.Locator("input[type='email']");
    private ILocator PasswordInput => _page.Locator("input[type='password']");
    private ILocator LoginButton => _page.Locator("button.login-btn");
    private ILocator GoogleLoginButton => _page.Locator("button.social-btn");

    public LoginPage(IPage page) : base(page)
    {
    }

    public async Task GotoAsync(string baseUrl)
    {
        await _page.GotoAsync($"{baseUrl}/login");
    }

    public async Task LoginWithCredentialsAsync(string email, string password)
    {
        await EmailInput.FillAsync(email);
        await PasswordInput.FillAsync(password);
        await LoginButton.ClickAsync();
    }
}
