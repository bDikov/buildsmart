using Microsoft.Extensions.Localization;
using BuildSmart.Core.Application.Interfaces;
using System.Collections.Concurrent;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;

namespace BuildSmart.SharedUI.Services.Localization;

public class DbStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly ILocalizationCacheService _cacheService;
    private readonly IStringLocalizerFactory _fallbackFactory;
    private readonly ConcurrentDictionary<string, IStringLocalizer> _localizers = new();

    public DbStringLocalizerFactory(ILocalizationCacheService cacheService, IOptions<LocalizationOptions> localizationOptions)
    {
        _cacheService = cacheService;
        _fallbackFactory = new ResourceManagerStringLocalizerFactory(localizationOptions, NullLoggerFactory.Instance);
    }

    public IStringLocalizer Create(Type resourceSource)
    {
        return _localizers.GetOrAdd(resourceSource.FullName ?? resourceSource.Name,
            _ => new DbStringLocalizer(_cacheService, _fallbackFactory.Create(resourceSource)));
    }

    public IStringLocalizer Create(string baseName, string location)
    {
        return _localizers.GetOrAdd(baseName,
            _ => new DbStringLocalizer(_cacheService, _fallbackFactory.Create(baseName, location)));
    }
}
