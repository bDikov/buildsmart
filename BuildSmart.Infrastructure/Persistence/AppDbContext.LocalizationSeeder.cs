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
        if (await LocalizationResources.AnyAsync())
        {
            return;
        }

        Console.WriteLine("Localization resources database table is empty. Seeding from assembly resources...");

        try
        {
            await SeedCultureFromManagerAsync(resourceManager, CultureInfo.InvariantCulture, "en");
            await SeedCultureFromManagerAsync(resourceManager, new CultureInfo("bg"), "bg");
            await SaveChangesAsync();
            Console.WriteLine("Localization resources seeding completed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error seeding localization resources: {ex.Message}");
        }
    }

    private async Task SeedCultureFromManagerAsync(ResourceManager resourceManager, CultureInfo cultureInfo, string cultureCode)
    {
        var resourceSet = resourceManager.GetResourceSet(cultureInfo, createIfNotExists: true, tryParents: true);
        if (resourceSet == null)
        {
            Console.WriteLine($"Warning: Resource set for culture '{cultureCode}' could not be loaded.");
            return;
        }

        foreach (DictionaryEntry entry in resourceSet)
        {
            var key = entry.Key?.ToString();
            var value = entry.Value?.ToString();

            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
            {
                var exists = await LocalizationResources.AnyAsync(r => r.Key == key && r.Culture == cultureCode);
                if (!exists)
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
                }
            }
        }
    }
}
