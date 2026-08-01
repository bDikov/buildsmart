using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BuildSmart.Api.Tests;

public class BlogSeederAndMediaServiceTests
{
    private DbContextOptions<AppDbContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"BlogTestsDb_{Guid.NewGuid()}")
            .Options;
    }

    [Fact]
    public async Task SeedBlogPostsAsync_PopulatesDatabase_WhenPostsJsonExists()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var tempDir = Path.Combine(Path.GetTempPath(), $"BlogSeederTest_{Guid.NewGuid()}");
        var postsDir = Path.Combine(tempDir, "posts");
        Directory.CreateDirectory(postsDir);

        var testSlug = "test-blog-post-2026";
        var jsonContent = $@"
[
  {{
    ""Slug"": ""{testSlug}"",
    ""TitleBg"": ""Заглавие на Български"",
    ""TitleEn"": ""English Title"",
    ""DescriptionBg"": ""Описание"",
    ""DescriptionEn"": ""Description"",
    ""Date"": ""2026-08-01"",
    ""CategoryBg"": ""Тест"",
    ""CategoryEn"": ""Test"",
    ""Image"": ""/images/blog/test.jpg"",
    ""ReadTimeBg"": ""3 мин."",
    ""ReadTimeEn"": ""3 min.""
  }}
]";
        await File.WriteAllTextAsync(Path.Combine(postsDir, "posts.json"), jsonContent);
        await File.WriteAllTextAsync(Path.Combine(postsDir, $"{testSlug}.bg.md"), "# Български Текст");
        await File.WriteAllTextAsync(Path.Combine(postsDir, $"{testSlug}.en.md"), "# English Content");

        try
        {
            // Act
            await using (var seedContext = new AppDbContext(options))
            {
                await seedContext.SeedBlogPostsAsync(tempDir);
            }

            // Assert
            await using (var assertContext = new AppDbContext(options))
            {
                var post = await assertContext.BlogPosts.FirstOrDefaultAsync(b => b.Slug.ToLower() == testSlug);
                Assert.NotNull(post);
                Assert.Equal("Заглавие на Български", post.TitleBg);
                Assert.Equal("English Title", post.TitleEn);
                Assert.Equal("# Български Текст", post.ContentBg);
                Assert.Equal("# English Content", post.ContentEn);
                Assert.True(post.IsPublished);
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task SeedBlogPostsAsync_UpdatesExistingEmptyContent()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var tempDir = Path.Combine(Path.GetTempPath(), $"BlogSeederTest_{Guid.NewGuid()}");
        var postsDir = Path.Combine(tempDir, "posts");
        Directory.CreateDirectory(postsDir);

        var testSlug = "existing-empty-post";
        var jsonContent = $@"
[
  {{
    ""Slug"": ""{testSlug}"",
    ""TitleBg"": ""Празен Пост"",
    ""TitleEn"": ""Empty Post""
  }}
]";
        await File.WriteAllTextAsync(Path.Combine(postsDir, "posts.json"), jsonContent);
        await File.WriteAllTextAsync(Path.Combine(postsDir, $"{testSlug}.bg.md"), "# Нов Допълнен Текст");

        // Pre-seed an entity with empty content in database
        await using (var setupContext = new AppDbContext(options))
        {
            await setupContext.BlogPosts.AddAsync(new BlogPost
            {
                Id = Guid.NewGuid(),
                Slug = testSlug,
                TitleBg = "Празен Пост",
                TitleEn = "Empty Post",
                ContentBg = "",
                ContentEn = "",
                PublishedAt = DateTime.UtcNow,
                IsPublished = true
            });
            await setupContext.SaveChangesAsync();
        }

        try
        {
            // Act
            await using (var seedContext = new AppDbContext(options))
            {
                await seedContext.SeedBlogPostsAsync(tempDir);
            }

            // Assert
            await using (var assertContext = new AppDbContext(options))
            {
                var post = await assertContext.BlogPosts.FirstOrDefaultAsync(b => b.Slug.ToLower() == testSlug);
                Assert.NotNull(post);
                Assert.Equal("# Нов Допълнен Текст", post.ContentBg);
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task BlogPosts_CaseInsensitiveLinqQuery_ReturnsCorrectEntity()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        var testSlug = "remont-na-banya-sofia-cena-2026";

        await using (var setupContext = new AppDbContext(options))
        {
            await setupContext.BlogPosts.AddAsync(new BlogPost
            {
                Id = Guid.NewGuid(),
                Slug = testSlug,
                TitleBg = "Тест Баня",
                TitleEn = "Test Bathroom",
                PublishedAt = DateTime.UtcNow,
                IsPublished = true
            });
            await setupContext.SaveChangesAsync();
        }

        // Act & Assert
        await using (var queryContext = new AppDbContext(options))
        {
            var uppercaseInput = "REMONT-NA-BANYA-SOFIA-CENA-2026";
            var targetSlug = uppercaseInput.ToLower().Trim();

            var result = await queryContext.BlogPosts
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Slug.ToLower() == targetSlug);

            Assert.NotNull(result);
            Assert.Equal(testSlug, result.Slug);
        }
    }
}
