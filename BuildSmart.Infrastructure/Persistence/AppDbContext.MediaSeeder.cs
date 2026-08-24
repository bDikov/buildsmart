using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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

        // --- 2. Synchronize Existing Media & Seed Assets ---
        await context.SyncAndSeedMediaAssetsAsync(existingFolders);
    }

    private static async Task SyncAndSeedMediaAssetsAsync(this AppDbContext context, List<MediaFolder> folders)
    {
        var existingAssets = await context.MediaAssets.ToListAsync();
        var assetsToInsert = new List<MediaAsset>();

        var feedFolder = folders.FirstOrDefault(f => f.Slug == "feed" && f.ParentId == null);
        var landingRoot = folders.FirstOrDefault(f => f.Slug == "landing-pages" && f.ParentId == null);
        var generalFolder = folders.FirstOrDefault(f => f.Slug == "general" && f.ParentId == null);
        var portfoliosFolder = folders.FirstOrDefault(f => f.Slug == "portfolios" && f.ParentId == null);
        var categoriesFolder = folders.FirstOrDefault(f => f.Slug == "categories" && f.ParentId == null);

        // A. Sync from TradesmanMedia
        if (feedFolder != null)
        {
            var tradesmanMediaList = await context.TradesmanMedia.AsNoTracking().ToListAsync();
            foreach (var tm in tradesmanMediaList)
            {
                if (!string.IsNullOrEmpty(tm.VideoUrl) && !existingAssets.Any(a => a.PublicUrl == tm.VideoUrl))
                {
                    var fileName = SafeGetFileName(tm.VideoUrl, $"feed-video-{tm.Id:N}.mp4");
                    var asset = new MediaAsset
                    {
                        Id = Guid.NewGuid(),
                        FolderId = feedFolder.Id,
                        FileName = fileName,
                        R2Key = $"feed/{fileName}",
                        PublicUrl = tm.VideoUrl,
                        ThumbnailUrl = !string.IsNullOrEmpty(tm.ThumbnailUrl) ? tm.ThumbnailUrl : tm.ImageUrl,
                        MediaType = "video",
                        ContentType = "video/mp4",
                        SizeBytes = 5242880, // Default estimate 5MB
                        CreatedAt = tm.CreatedAt,
                        UpdatedAt = tm.UpdatedAt
                    };
                    assetsToInsert.Add(asset);
                    existingAssets.Add(asset);
                }

                if (!string.IsNullOrEmpty(tm.ImageUrl) && tm.ImageUrl != tm.VideoUrl && !existingAssets.Any(a => a.PublicUrl == tm.ImageUrl))
                {
                    var fileName = SafeGetFileName(tm.ImageUrl, $"feed-thumb-{tm.Id:N}.webp");
                    var asset = new MediaAsset
                    {
                        Id = Guid.NewGuid(),
                        FolderId = feedFolder.Id,
                        FileName = fileName,
                        R2Key = $"feed/{fileName}",
                        PublicUrl = tm.ImageUrl,
                        ThumbnailUrl = tm.ImageUrl,
                        MediaType = "image",
                        ContentType = "image/webp",
                        SizeBytes = 350000,
                        CreatedAt = tm.CreatedAt,
                        UpdatedAt = tm.UpdatedAt
                    };
                    assetsToInsert.Add(asset);
                    existingAssets.Add(asset);
                }
            }
        }

        // B. Sync from LandingPages
        var landingPages = await context.LandingPages.AsNoTracking().ToListAsync();
        foreach (var lp in landingPages)
        {
            var targetFolder = folders.FirstOrDefault(f => f.Slug == lp.Slug && f.ParentId == landingRoot?.Id) ?? landingRoot ?? generalFolder;
            if (targetFolder == null) continue;

            if (!string.IsNullOrEmpty(lp.HeroImageUrl) && !existingAssets.Any(a => a.PublicUrl == lp.HeroImageUrl))
            {
                var fileName = $"hero-{lp.Slug}.jpg";
                var asset = new MediaAsset
                {
                    Id = Guid.NewGuid(),
                    FolderId = targetFolder.Id,
                    FileName = fileName,
                    R2Key = $"landing-pages/{lp.Slug}/{fileName}",
                    PublicUrl = lp.HeroImageUrl,
                    ThumbnailUrl = lp.HeroImageUrl,
                    MediaType = "image",
                    ContentType = "image/jpeg",
                    AltTextBg = lp.TitleBg,
                    AltTextEn = lp.TitleEn,
                    SizeBytes = 850000,
                    CreatedAt = lp.CreatedAt,
                    UpdatedAt = lp.UpdatedAt
                };
                assetsToInsert.Add(asset);
                existingAssets.Add(asset);
            }

            if (!string.IsNullOrEmpty(lp.HeroVideoUrl) && !existingAssets.Any(a => a.PublicUrl == lp.HeroVideoUrl))
            {
                var fileName = $"hero-video-{lp.Slug}.mp4";
                var asset = new MediaAsset
                {
                    Id = Guid.NewGuid(),
                    FolderId = targetFolder.Id,
                    FileName = fileName,
                    R2Key = $"landing-pages/{lp.Slug}/{fileName}",
                    PublicUrl = lp.HeroVideoUrl,
                    ThumbnailUrl = null,
                    MediaType = "video",
                    ContentType = "video/mp4",
                    SizeBytes = 12500000,
                    CreatedAt = lp.CreatedAt,
                    UpdatedAt = lp.UpdatedAt
                };
                assetsToInsert.Add(asset);
                existingAssets.Add(asset);
            }

            if (!string.IsNullOrWhiteSpace(lp.MediaGalleryJson) && lp.MediaGalleryJson.Trim() != "[]")
            {
                try
                {
                    using var doc = JsonDocument.Parse(lp.MediaGalleryJson);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        int gIdx = 1;
                        foreach (var item in doc.RootElement.EnumerateArray())
                        {
                            var url = item.TryGetProperty("url", out var u) ? u.GetString() : null;
                            if (!string.IsNullOrEmpty(url) && !existingAssets.Any(a => a.PublicUrl == url))
                            {
                                var type = item.TryGetProperty("type", out var t) ? t.GetString() : "image";
                                var capBg = item.TryGetProperty("captionBg", out var cbg) ? cbg.GetString() : null;
                                var capEn = item.TryGetProperty("captionEn", out var cen) ? cen.GetString() : null;
                                var fileName = $"gallery-{lp.Slug}-{gIdx++}.jpg";

                                var asset = new MediaAsset
                                {
                                    Id = Guid.NewGuid(),
                                    FolderId = targetFolder.Id,
                                    FileName = fileName,
                                    R2Key = $"landing-pages/{lp.Slug}/{fileName}",
                                    PublicUrl = url,
                                    ThumbnailUrl = url,
                                    MediaType = type ?? "image",
                                    ContentType = type == "video" ? "video/mp4" : "image/jpeg",
                                    AltTextBg = capBg,
                                    AltTextEn = capEn,
                                    SizeBytes = 650000,
                                    CreatedAt = lp.CreatedAt,
                                    UpdatedAt = lp.UpdatedAt
                                };
                                assetsToInsert.Add(asset);
                                existingAssets.Add(asset);
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore JSON parse errors for user-modified gallery
                }
            }
        }

        // C. Default Stock Renovation & Demo Assets
        var stockItems = new[]
        {
            (
                Folder: feedFolder,
                Url: "https://assets.mixkit.co/videos/preview/mixkit-modern-apartment-interior-design-41558-large.mp4",
                Thumb: "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?auto=format&fit=crop&w=800&q=80",
                Type: "video",
                FileName: "modern-apartment-tour.mp4",
                AltBg: "Видео обиколка на завършен апартамент",
                AltEn: "Modern apartment video tour",
                Size: 8400000L
            ),
            (
                Folder: portfoliosFolder,
                Url: "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?auto=format&fit=crop&w=1920&q=80",
                Thumb: "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?auto=format&fit=crop&w=600&q=80",
                Type: "image",
                FileName: "luxury-living-room-renovation.webp",
                AltBg: "Цялостен ремонт на хол и дневна",
                AltEn: "Luxury living room turnkey renovation",
                Size: 1420000L
            ),
            (
                Folder: portfoliosFolder,
                Url: "https://images.unsplash.com/photo-1584622650111-993a426fbf0a?auto=format&fit=crop&w=1920&q=80",
                Thumb: "https://images.unsplash.com/photo-1584622650111-993a426fbf0a?auto=format&fit=crop&w=600&q=80",
                Type: "image",
                FileName: "porcelain-bathroom-finish.webp",
                AltBg: "Модерен ремонт на баня с гранитогрес",
                AltEn: "Modern porcelain bathroom tiling",
                Size: 980000L
            ),
            (
                Folder: categoriesFolder,
                Url: "https://images.unsplash.com/photo-1513694203232-719a280e022f?auto=format&fit=crop&w=1920&q=80",
                Thumb: "https://images.unsplash.com/photo-1513694203232-719a280e022f?auto=format&fit=crop&w=600&q=80",
                Type: "image",
                FileName: "painting-drywall-finishing.webp",
                AltBg: "Шпакловка, боядисване и гипсокартон",
                AltEn: "Plaster skimming, painting and drywall",
                Size: 1100000L
            ),
            (
                Folder: categoriesFolder,
                Url: "https://images.unsplash.com/photo-1621905251189-08b45d6a269e?auto=format&fit=crop&w=1920&q=80",
                Thumb: "https://images.unsplash.com/photo-1621905251189-08b45d6a269e?auto=format&fit=crop&w=600&q=80",
                Type: "image",
                FileName: "electrical-and-plumbing-service.webp",
                AltBg: "Електро и ВиК инсталации София",
                AltEn: "Electrical and plumbing installations",
                Size: 1250000L
            ),
            (
                Folder: generalFolder,
                Url: "https://images.unsplash.com/photo-1503387762-592deb58ef4e?auto=format&fit=crop&w=1920&q=80",
                Thumb: "https://images.unsplash.com/photo-1503387762-592deb58ef4e?auto=format&fit=crop&w=600&q=80",
                Type: "image",
                FileName: "architectural-blueprint-planning.webp",
                AltBg: "Архитектурен план и строителен проект",
                AltEn: "Architectural blueprint and site plan",
                Size: 1350000L
            )
        };

        foreach (var item in stockItems)
        {
            if (item.Folder != null && !existingAssets.Any(a => a.PublicUrl == item.Url))
            {
                var asset = new MediaAsset
                {
                    Id = Guid.NewGuid(),
                    FolderId = item.Folder.Id,
                    FileName = item.FileName,
                    R2Key = $"{item.Folder.FullPath.Trim('/')}/{item.FileName}",
                    PublicUrl = item.Url,
                    ThumbnailUrl = item.Thumb,
                    MediaType = item.Type,
                    ContentType = item.Type == "video" ? "video/mp4" : "image/webp",
                    AltTextBg = item.AltBg,
                    AltTextEn = item.AltEn,
                    SizeBytes = item.Size,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                assetsToInsert.Add(asset);
                existingAssets.Add(asset);
            }
        }

        if (assetsToInsert.Count > 0)
        {
            await context.MediaAssets.AddRangeAsync(assetsToInsert);
            await context.SaveChangesAsync();
        }
    }

    private static string SafeGetFileName(string url, string fallback)
    {
        try
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                var name = Path.GetFileName(uri.AbsolutePath);
                if (!string.IsNullOrWhiteSpace(name)) return name;
            }
        }
        catch
        {
            // Ignore
        }
        return fallback;
    }
}
