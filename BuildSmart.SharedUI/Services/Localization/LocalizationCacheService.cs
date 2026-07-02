using BuildSmart.Core.Application.Interfaces;
using System.Collections.Concurrent;

namespace BuildSmart.SharedUI.Services.Localization;

public class LocalizationCacheService : ILocalizationCacheService
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _cache = new(StringComparer.OrdinalIgnoreCase);

    public string? Get(string key, string culture)
    {
        if (_cache.TryGetValue(culture, out var cultureDict))
        {
            if (cultureDict.TryGetValue(key, out var value))
            {
                return value;
            }
        }
        return null;
    }

    public void Set(string key, string culture, string value)
    {
        var cultureDict = _cache.GetOrAdd(culture, _ => new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        cultureDict[key] = value;
    }

    public void Initialize(Dictionary<string, Dictionary<string, string>> values)
    {
        _cache.Clear();
        foreach (var culturePair in values)
        {
            var cultureDict = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var keyValuePair in culturePair.Value)
            {
                cultureDict[keyValuePair.Key] = keyValuePair.Value;
            }
            _cache[culturePair.Key] = cultureDict;
        }
    }

    public Dictionary<string, string> GetValuesForCulture(string culture)
    {
        if (_cache.TryGetValue(culture, out var cultureDict))
        {
            return new Dictionary<string, string>(cultureDict, StringComparer.OrdinalIgnoreCase);
        }
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
