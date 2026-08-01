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
            var possiblePaths = new[]
            {
                Path.Combine(webRootPath ?? "", "posts", "posts.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "posts", "posts.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "BuildSmart.Web", "wwwroot", "posts", "posts.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "BuildSmart.Web", "wwwroot", "posts", "posts.json")
            };

            var postsFilePath = possiblePaths.FirstOrDefault(File.Exists);
            if (string.IsNullOrEmpty(postsFilePath))
            {
                Console.WriteLine("posts.json not found in any search path.");
                return;
            }

            var postsDir = Path.GetDirectoryName(postsFilePath)!;
            var jsonContent = await File.ReadAllTextAsync(postsFilePath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var jsonPosts = JsonSerializer.Deserialize<List<JsonBlogPostSeedDto>>(jsonContent, options);

            if (jsonPosts == null || !jsonPosts.Any())
            {
                return;
            }

            foreach (var dto in jsonPosts)
            {
                if (string.IsNullOrWhiteSpace(dto.Slug)) continue;

                var bgMdPath = Path.Combine(postsDir, $"{dto.Slug}.bg.md");
                var defMdPath = Path.Combine(postsDir, $"{dto.Slug}.md");
                var enMdPath = Path.Combine(postsDir, $"{dto.Slug}.en.md");

                var existing = await BlogPosts.FirstOrDefaultAsync(b => b.Slug == dto.Slug);
                if (existing != null)
                {
                    bool updated = false;
                    if (string.IsNullOrWhiteSpace(existing.ContentBg))
                    {
                        if (File.Exists(bgMdPath)) existing.ContentBg = await File.ReadAllTextAsync(bgMdPath);
                        else if (File.Exists(defMdPath)) existing.ContentBg = await File.ReadAllTextAsync(defMdPath);
                        updated = true;
                    }
                    if (string.IsNullOrWhiteSpace(existing.ContentEn))
                    {
                        if (File.Exists(enMdPath)) existing.ContentEn = await File.ReadAllTextAsync(enMdPath);
                        updated = true;
                    }
                    if (updated)
                    {
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                    continue;
                }

                string contentBg = "";
                string contentEn = "";

                if (File.Exists(bgMdPath)) contentBg = await File.ReadAllTextAsync(bgMdPath);
                else if (File.Exists(defMdPath)) contentBg = await File.ReadAllTextAsync(defMdPath);

                if (File.Exists(enMdPath)) contentEn = await File.ReadAllTextAsync(enMdPath);

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
