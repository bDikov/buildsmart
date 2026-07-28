using Microsoft.Extensions.Localization;
using BuildSmart.Core.Application.Interfaces;
using System.Globalization;

namespace BuildSmart.SharedUI.Services.Localization;

public class DbStringLocalizer : IStringLocalizer
{
    private readonly ILocalizationCacheService _cacheService;

    private readonly IStringLocalizer _fallbackLocalizer;

    public DbStringLocalizer(ILocalizationCacheService cacheService, IStringLocalizer fallbackLocalizer)
    {
        _cacheService = cacheService;
        _fallbackLocalizer = fallbackLocalizer;
    }

    public LocalizedString this[string name]
    {
        get
        {
            var value = GetString(name);
            if (value != null)
            {
                return new LocalizedString(name, value, resourceNotFound: false);
            }
            return _fallbackLocalizer[name];
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var format = GetString(name);
            if (format != null)
            {
                var value = string.Format(format, arguments);
                return new LocalizedString(name, value, resourceNotFound: false);
            }
            return _fallbackLocalizer[name, arguments];
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var culture = CultureInfo.CurrentUICulture.Name;
        var resources = _cacheService.GetValuesForCulture(culture);
        return resources.Select(r => new LocalizedString(r.Key, r.Value, false));
    }

    private string? GetString(string key)
    {
        var culture = CultureInfo.CurrentUICulture.Name;
        var parentCulture = CultureInfo.CurrentUICulture.Parent.Name;

        var value = _cacheService.Get(key, culture);
        if (value != null) return value;

        if (!string.IsNullOrEmpty(parentCulture))
        {
            value = _cacheService.Get(key, parentCulture);
            if (value != null) return value;
        }

        // Only fall back to English if the active culture is not Bulgarian
        var isBulgarian = culture.StartsWith("bg", StringComparison.OrdinalIgnoreCase) || 
                          parentCulture.StartsWith("bg", StringComparison.OrdinalIgnoreCase);

        // Fallback for built-in JobWizard keys if missing in DB cache
        if (isBulgarian)
        {
            if (key == "JobWizard_No") return "Не";
            if (key == "JobWizard_Yes") return "Да";
            if (key == "JobWizard_SaveAndViewOffer") return "Запази и виж офертата";
            if (key == "JobWizard_TypeAnswerHere") return "Въведете вашия отговор тук...";
            if (key == "JobWizard_CategoryLabel") return "Изберете категория услуга";
            if (key == "JobWizard_SelectOption") return "Изберете опция";
        }
        else
        {
            if (key == "JobWizard_No") return "No";
            if (key == "JobWizard_Yes") return "Yes";
            if (key == "JobWizard_SaveAndViewOffer") return "Save & View Offer";
            if (key == "JobWizard_TypeAnswerHere") return "Type your answer here...";
            if (key == "JobWizard_CategoryLabel") return "Select service category";
            if (key == "JobWizard_SelectOption") return "Select an option";

            return _cacheService.Get(key, "en");
        }

        return null;
    }
}
