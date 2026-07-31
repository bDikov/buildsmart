using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Globalization;
using System.Resources;
using System.Threading.Tasks;

namespace BuildSmart.Infrastructure.Persistence;

public partial class AppDbContext
{
    public async Task SeedLocalizationResourcesAsync(ResourceManager resourceManager)
    {
        Console.WriteLine("Checking for missing localization resources in database...");

        try
        {
            // Load existing entries into a Dictionary for fast in-memory lookup and value comparison
            var existingEntries = await LocalizationResources
                .Select(r => new { r.Key, r.Culture, r.Value })
                .ToListAsync();
            
            var existingMap = existingEntries.ToDictionary(
                e => (e.Key.ToLowerInvariant(), e.Culture.ToLowerInvariant()),
                e => e.Value ?? string.Empty
            );

            bool addedAny = false;
            addedAny |= await SeedCultureFromManagerAsync(resourceManager, CultureInfo.InvariantCulture, "en", existingMap);
            addedAny |= await SeedCultureFromManagerAsync(resourceManager, new CultureInfo("bg"), "bg", existingMap);

            if (addedAny)
            {
                await SaveChangesAsync();
                Console.WriteLine("Localization resources seeding completed successfully.");
            }
            else
            {
                Console.WriteLine("No new or modified localization resources to seed.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error seeding localization resources: {ex.Message}");
        }
    }

    private async Task<bool> SeedCultureFromManagerAsync(
        ResourceManager resourceManager, 
        CultureInfo cultureInfo, 
        string cultureCode, 
        Dictionary<(string key, string culture), string> existingMap)
    {
        var resourceSet = resourceManager.GetResourceSet(cultureInfo, createIfNotExists: true, tryParents: true);
        if (resourceSet == null)
        {
            Console.WriteLine($"Warning: Resource set for culture '{cultureCode}' could not be loaded.");
            return false;
        }

        bool added = false;
        foreach (DictionaryEntry entry in resourceSet)
        {
            var key = entry.Key?.ToString();
            var value = entry.Value?.ToString();

            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
            {
                var lookupKey = (key.ToLowerInvariant(), cultureCode.ToLowerInvariant());
                if (!existingMap.TryGetValue(lookupKey, out var existingValue))
                {
                    var resource = new LocalizationResource
                    {
                        Id = Guid.NewGuid(),
                        Key = key,
                        Culture = cultureCode,
                        Value = value,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await LocalizationResources.AddAsync(resource);
                    existingMap[lookupKey] = value;
                    added = true;
                }
                else if (existingValue != value)
                {
                    var existingEntity = await LocalizationResources.FirstOrDefaultAsync(r => r.Key == key && r.Culture == cultureCode);
                    if (existingEntity != null)
                    {
                        existingEntity.Value = value;
                        existingEntity.UpdatedAt = DateTime.UtcNow;
                        existingMap[lookupKey] = value;
                        added = true;
                    }
                }
            }
        }
        return added;
    }
}
