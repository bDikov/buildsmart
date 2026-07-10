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
            // Load existing entries into a HashSet for fast in-memory lookup
            var existingEntries = await LocalizationResources
                .Select(r => new { r.Key, r.Culture })
                .ToListAsync();
            
            var existingSet = new HashSet<(string key, string culture)>(
                existingEntries.Select(e => (e.Key.ToLowerInvariant(), e.Culture.ToLowerInvariant()))
            );

            bool addedAny = false;
            addedAny |= await SeedCultureFromManagerAsync(resourceManager, CultureInfo.InvariantCulture, "en", existingSet);
            addedAny |= await SeedCultureFromManagerAsync(resourceManager, new CultureInfo("bg"), "bg", existingSet);

            if (addedAny)
            {
                await SaveChangesAsync();
                Console.WriteLine("Localization resources seeding completed successfully.");
            }
            else
            {
                Console.WriteLine("No new localization resources to seed.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error seeding localization resources: {ex.Message}");
        }
    }

    private async Task<bool> SeedCultureFromManagerAsync(ResourceManager resourceManager, CultureInfo cultureInfo, string cultureCode, HashSet<(string key, string culture)> existingSet)
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
                if (!existingSet.Contains(lookupKey))
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
                    existingSet.Add(lookupKey);
                    added = true;
                }
            }
        }
        return added;
    }
}
