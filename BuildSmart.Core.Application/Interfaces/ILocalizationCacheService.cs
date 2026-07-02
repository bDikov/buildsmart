namespace BuildSmart.Core.Application.Interfaces;

public interface ILocalizationCacheService
{
    string? Get(string key, string culture);
    void Set(string key, string culture, string value);
    void Initialize(Dictionary<string, Dictionary<string, string>> values);
    Dictionary<string, string> GetValuesForCulture(string culture);
}
