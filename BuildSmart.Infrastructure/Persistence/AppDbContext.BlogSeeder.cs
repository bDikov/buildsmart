using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BuildSmart.Infrastructure.Persistence;

public partial class AppDbContext
{
    public async Task SeedBlogPostsAsync(string webRootPath)
    {
        try
        {
            Console.WriteLine("Checking for missing blog posts in PostgreSQL database...");

            var searchDirs = new List<string>();
            var seedPostsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "seed_posts");
            if (Directory.Exists(seedPostsDir)) searchDirs.Add(seedPostsDir);

            if (!string.IsNullOrEmpty(webRootPath))
            {
                var webPosts = Path.Combine(webRootPath, "posts");
                if (Directory.Exists(webPosts) && !searchDirs.Contains(webPosts)) searchDirs.Add(webPosts);
            }

            var currentWwwrootPosts = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "posts");
            if (Directory.Exists(currentWwwrootPosts) && !searchDirs.Contains(currentWwwrootPosts)) searchDirs.Add(currentWwwrootPosts);

            var parentWwwrootPosts = Path.Combine(Directory.GetCurrentDirectory(), "..", "BuildSmart.Web", "wwwroot", "posts");
            if (Directory.Exists(parentWwwrootPosts) && !searchDirs.Contains(parentWwwrootPosts)) searchDirs.Add(parentWwwrootPosts);

            var buildsmartWebPosts = Path.Combine(Directory.GetCurrentDirectory(), "BuildSmart.Web", "wwwroot", "posts");
            if (Directory.Exists(buildsmartWebPosts) && !searchDirs.Contains(buildsmartWebPosts)) searchDirs.Add(buildsmartWebPosts);

            if (!searchDirs.Any())
            {
                Console.WriteLine("No posts directory found in any search path.");
                return;
            }

            var mergedPosts = new Dictionary<string, JsonBlogPostSeedDto>(StringComparer.OrdinalIgnoreCase);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var dir in searchDirs)
            {
                var jsonPath = Path.Combine(dir, "posts.json");
                if (File.Exists(jsonPath))
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(jsonPath);
                        var list = JsonSerializer.Deserialize<List<JsonBlogPostSeedDto>>(json, options);
                        if (list != null)
                        {
                            foreach (var p in list)
                            {
                                if (!string.IsNullOrWhiteSpace(p.Slug) && !mergedPosts.ContainsKey(p.Slug))
                                {
                                    mergedPosts[p.Slug] = p;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error reading posts.json in {dir}: {ex.Message}");
                    }
                }
            }

            if (!mergedPosts.Any())
            {
                Console.WriteLine("No valid posts found to seed.");
                return;
            }

            // Sync missing markdown and updated posts.json to web volume if applicable
            if (!string.IsNullOrEmpty(webRootPath))
            {
                var targetWebPostsDir = Path.Combine(webRootPath, "posts");
                if (Directory.Exists(targetWebPostsDir) && Directory.Exists(seedPostsDir))
                {
                    try
                    {
                        foreach (var srcFile in Directory.GetFiles(seedPostsDir, "*.md"))
                        {
                            var targetFile = Path.Combine(targetWebPostsDir, Path.GetFileName(srcFile));
                            if (!File.Exists(targetFile))
                            {
                                File.Copy(srcFile, targetFile, overwrite: false);
                            }
                        }

                        var targetJsonPath = Path.Combine(targetWebPostsDir, "posts.json");
                        var writeOptions = new JsonSerializerOptions { WriteIndented = true };
                        var serialized = JsonSerializer.Serialize(mergedPosts.Values.ToList(), writeOptions);
                        await File.WriteAllTextAsync(targetJsonPath, serialized);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Note: Unable to sync files to web volume: {ex.Message}");
                    }
                }
            }

            foreach (var dto in mergedPosts.Values)
            {
                if (string.IsNullOrWhiteSpace(dto.Slug)) continue;

                string? FindFile(string filename)
                {
                    foreach (var d in searchDirs)
                    {
                        var p = Path.Combine(d, filename);
                        if (File.Exists(p)) return p;
                    }
                    return null;
                }

                var bgMdPath = FindFile($"{dto.Slug}.bg.md") ?? FindFile($"{dto.Slug}.md");
                var enMdPath = FindFile($"{dto.Slug}.en.md");

                var existing = await BlogPosts.FirstOrDefaultAsync(b => b.Slug == dto.Slug);
                if (existing != null)
                {
                    bool updated = false;
                    if (string.IsNullOrWhiteSpace(existing.ContentBg) && bgMdPath != null)
                    {
                        existing.ContentBg = await File.ReadAllTextAsync(bgMdPath);
                        updated = true;
                    }
                    if (string.IsNullOrWhiteSpace(existing.ContentEn) && enMdPath != null)
                    {
                        existing.ContentEn = await File.ReadAllTextAsync(enMdPath);
                        updated = true;
                    }
                    if (string.IsNullOrWhiteSpace(existing.CoverImageUrl) && !string.IsNullOrWhiteSpace(dto.Image))
                    {
                        existing.CoverImageUrl = dto.Image;
                        updated = true;
                    }
                    if (updated)
                    {
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                    continue;
                }

                string contentBg = bgMdPath != null ? await File.ReadAllTextAsync(bgMdPath) : "";
                string contentEn = enMdPath != null ? await File.ReadAllTextAsync(enMdPath) : "";

                var post = new BlogPost
                {
                    Id = Guid.NewGuid(),
                    Slug = dto.Slug,
                    TitleBg = dto.TitleBg ?? dto.Title ?? "",
                    TitleEn = dto.TitleEn ?? "",
                    DescriptionBg = dto.DescriptionBg ?? dto.Description ?? "",
                    DescriptionEn = dto.DescriptionEn ?? "",
                    ContentBg = contentBg,
                    ContentEn = contentEn,
                    CoverImageUrl = dto.Image ?? "",
                    CategoryBg = dto.CategoryBg ?? dto.Category ?? "Общи",
                    CategoryEn = dto.CategoryEn ?? dto.Category ?? "General",
                    ReadTimeBg = dto.ReadTimeBg ?? dto.ReadTime ?? "3 мин.",
                    ReadTimeEn = dto.ReadTimeEn ?? dto.ReadTime ?? "3 min.",
                    SeoKeywordsBg = dto.SeoKeywordsBg ?? dto.SeoKeywords,
                    SeoKeywordsEn = dto.SeoKeywordsEn ?? dto.SeoKeywords,
                    PublishedAt = DateTime.TryParse(dto.Date, out var dt) ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsPublished = true
                };

                await BlogPosts.AddAsync(post);
            }

            await SaveChangesAsync();
            Console.WriteLine("Successfully seeded blog posts into PostgreSQL!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error seeding blog posts: {ex.Message}");
        }
    }

    private class JsonBlogPostSeedDto
    {
        public string Slug { get; set; } = "";
        public string? TitleBg { get; set; }
        public string? TitleEn { get; set; }
        public string? DescriptionBg { get; set; }
        public string? DescriptionEn { get; set; }
        public string? Date { get; set; }
        public string? CategoryBg { get; set; }
        public string? CategoryEn { get; set; }
        public string? Image { get; set; }
        public string? ReadTimeBg { get; set; }
        public string? ReadTimeEn { get; set; }
        public string? SeoKeywordsBg { get; set; }
        public string? SeoKeywordsEn { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? ReadTime { get; set; }
        public string? SeoKeywords { get; set; }
    }
}
