using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuildSmart.Api.DTOs;
using BuildSmart.Api.GraphQL;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BuildSmart.Api.Tests;

public class LandingPageCmsTests
{
    private AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"LandingPageCmsDb_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task SeedLandingPagesAsync_SeedsDefaultLandingPages()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        // Act
        await context.SeedLandingPagesAsync();

        // Assert
        var pages = await context.LandingPages.ToListAsync();
        Assert.Equal(4, pages.Count);
        Assert.Contains(pages, p => p.Slug == "remont-na-apartament-sofia");
        Assert.Contains(pages, p => p.Slug == "remont-na-banya");
        Assert.Contains(pages, p => p.Slug == "dovarshetelni-raboti");
        Assert.Contains(pages, p => p.Slug == "el-i-vik-uslugi");
    }

    [Fact]
    public async Task GetLandingPageBySlug_ReturnsCorrectPage()
    {
        // Arrange
        using var seedingContext = CreateInMemoryDbContext();
        await seedingContext.SeedLandingPagesAsync();

        using var queryContext = CreateInMemoryDbContext();

        // Seed target item
        var testPage = new LandingPageContent
        {
            Id = Guid.NewGuid(),
            Slug = "custom-test-landing-page",
            TitleBg = "Тестова Страница",
            TitleEn = "Test Page",
            HeroImageUrl = "/images/test.jpg",
            HeroVideoUrl = "https://cdn.example.com/test.mp4"
        };
        await queryContext.LandingPages.AddAsync(testPage);
        await queryContext.SaveChangesAsync();

        var query = new Query();

        // Act
        var result = await query.GetLandingPageBySlug("custom-test-landing-page", queryContext, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("custom-test-landing-page", result.Slug);
        Assert.Equal("/images/test.jpg", result.HeroImageUrl);
        Assert.Equal("https://cdn.example.com/test.mp4", result.HeroVideoUrl);
    }

    [Fact]
    public async Task UpsertLandingPage_CreatesAndUpdatesLandingPage()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mutation = new Mutation();

        var input = new LandingPageInput
        {
            Slug = "new-seo-landing-page",
            PageType = "custom",
            TitleBg = "Заглавие БГ",
            TitleEn = "Title EN",
            HeroImageUrl = "/images/hero.jpg",
            HeroVideoUrl = "https://cdn.example.com/hero.mp4",
            MediaGalleryJson = "[{\"url\":\"/gallery-1.jpg\",\"type\":\"image\"}]",
            IsPublished = true
        };

        // Act - Create
        var created = await mutation.UpsertLandingPage(input, context, CancellationToken.None);

        // Assert - Create
        Assert.NotNull(created);
        Assert.Equal("new-seo-landing-page", created.Slug);
        Assert.Equal("/images/hero.jpg", created.HeroImageUrl);

        // Act - Update
        input.Id = created.Id;
        input.TitleBg = "Обновено Заглавие БГ";
        var updated = await mutation.UpsertLandingPage(input, context, CancellationToken.None);

        // Assert - Update
        Assert.Equal("Обновено Заглавие БГ", updated.TitleBg);
        Assert.Equal(created.Id, updated.Id);
    }

    [Fact]
    public async Task DeleteLandingPage_RemovesPageFromDatabase()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var page = new LandingPageContent
        {
            Id = Guid.NewGuid(),
            Slug = "page-to-delete",
            TitleBg = "За изтриване",
            TitleEn = "To Delete"
        };
        await context.LandingPages.AddAsync(page);
        await context.SaveChangesAsync();

        var mutation = new Mutation();

        // Act
        var success = await mutation.DeleteLandingPage(page.Id, context, CancellationToken.None);

        // Assert
        Assert.True(success);
        var deleted = await context.LandingPages.FirstOrDefaultAsync(p => p.Id == page.Id);
        Assert.Null(deleted);
    }
}
