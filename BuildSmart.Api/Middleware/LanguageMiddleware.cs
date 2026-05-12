using System.Globalization;

namespace BuildSmart.Api.Middleware;

public class LanguageMiddleware
{
    private readonly RequestDelegate _next;

    public LanguageMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var languageHeader = context.Request.Headers["Accept-Language"].ToString();
        var languageCode = "en"; // Default

        if (!string.IsNullOrEmpty(languageHeader))
        {
            var languages = languageHeader.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (languages.Length > 0)
            {
                var primaryLanguage = languages[0].Split(';')[0].Trim();
                if (primaryLanguage.StartsWith("bg", StringComparison.OrdinalIgnoreCase))
                {
                    languageCode = "bg";
                }
                else
                {
                    // keep first 2 letters for consistency
                    languageCode = primaryLanguage.Length >= 2 ? primaryLanguage.Substring(0, 2).ToLower() : "en";
                }
            }
        }

        context.Items["LanguageCode"] = languageCode;

        try
        {
            var culture = new CultureInfo(languageCode);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }
        catch (CultureNotFoundException)
        {
            // Ignore invalid cultures
        }

        await _next(context);
    }
}