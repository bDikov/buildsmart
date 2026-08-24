using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildSmart.Infrastructure.Persistence;

public static class MediaSeeder
{
    public static async Task SeedMediaFoldersAsync(this AppDbContext context)
    {
        var existingFolders = await context.MediaFolders.ToListAsync();

        var rootFolders = new (string Name, string Slug, string Path)[]
        {
            ("Landing Pages", "landing-pages", "/landing-pages"),
            ("Tradesman Feed & Videos", "feed", "/feed"),
            ("Categories & SKUs", "categories", "/categories"),
            ("Portfolios", "portfolios", "/portfolios"),
            ("Certifications", "certifications", "/certifications"),
            ("General Uploads", "general", "/general")
        };

        var landingSubFolders = new (string Name, string Slug, string Path)[]
        {
            ("Remont na Apartament Sofia", "remont-na-apartament-sofia", "/landing-pages/remont-na-apartament-sofia"),
            ("Remont na Banya", "remont-na-banya", "/landing-pages/remont-na-banya"),
            ("Dovarshetelni Raboti", "dovarshetelni-raboti", "/landing-pages/dovarshetelni-raboti"),
            ("El i ViK Uslugi", "el-i-vik-uslugi", "/landing-pages/el-i-vik-uslugi")
        };

        var foldersToInsert = new List<MediaFolder>();

        foreach (var rf in rootFolders)
        {
            var folder = existingFolders.FirstOrDefault(f => f.ParentId == null && f.Slug == rf.Slug);
            if (folder == null)
            {
                folder = new MediaFolder
                {
                    Id = Guid.NewGuid(),
                    ParentId = null,
                    Name = rf.Name,
                    Slug = rf.Slug,
                    FullPath = rf.Path,
                    IsSystem = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                foldersToInsert.Add(folder);
                existingFolders.Add(folder);
            }
        }

        if (foldersToInsert.Count > 0)
        {
            await context.MediaFolders.AddRangeAsync(foldersToInsert);
            await context.SaveChangesAsync();
            foldersToInsert.Clear();
        }

        var landingRoot = existingFolders.FirstOrDefault(f => f.ParentId == null && f.Slug == "landing-pages");
        if (landingRoot != null)
        {
            foreach (var sf in landingSubFolders)
            {
                var sub = existingFolders.FirstOrDefault(f => f.ParentId == landingRoot.Id && f.Slug == sf.Slug);
                if (sub == null)
                {
                    sub = new MediaFolder
                    {
                        Id = Guid.NewGuid(),
                        ParentId = landingRoot.Id,
                        Name = sf.Name,
                        Slug = sf.Slug,
                        FullPath = sf.Path,
                        IsSystem = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    foldersToInsert.Add(sub);
                    existingFolders.Add(sub);
                }
            }

            if (foldersToInsert.Count > 0)
            {
                await context.MediaFolders.AddRangeAsync(foldersToInsert);
                await context.SaveChangesAsync();
            }
        }
    }
}
