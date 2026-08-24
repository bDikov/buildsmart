using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Infrastructure.Persistence;
using BuildSmart.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BuildSmart.Api.Tests.Services;

public class UnifiedMediaServiceTests
{
    private AppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    private IConfiguration CreateConfiguration()
    {
        var inMemorySettings = new System.Collections.Generic.Dictionary<string, string?>
        {
            {"CloudflareR2:AccessKey", "test-access-key"},
            {"CloudflareR2:SecretKey", "test-secret-key"},
            {"CloudflareR2:BucketName", "test-bucket"},
            {"CloudflareR2:ServiceUrl", "https://test.r2.cloudflarestorage.com"},
            {"CloudflareR2:PublicUrl", "https://pub-test.r2.dev"}
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    [Fact]
    public async Task CreateFolderAsync_CreatesRootAndSubFolders_WithCorrectPaths()
    {
        var dbName = $"UnifiedMediaDb_{Guid.NewGuid()}";
        var config = CreateConfiguration();
        var mockImageService = new Mock<IImageProcessingService>();

        // 1. Create root folder
        using (var context = CreateDbContext(dbName))
        {
            var service = new UnifiedMediaService(context, config, NullLogger<UnifiedMediaService>.Instance, mockImageService.Object);
            var root = await service.CreateFolderAsync("Landing Pages");
            Assert.NotNull(root);
            Assert.Equal("Landing Pages", root.Name);
            Assert.Equal("landing-pages", root.Slug);
            Assert.Equal("/landing-pages", root.FullPath);
            Assert.Null(root.ParentId);
        }

        // 2. Create subfolder in separate context
        using (var context = CreateDbContext(dbName))
        {
            var service = new UnifiedMediaService(context, config, NullLogger<UnifiedMediaService>.Instance, mockImageService.Object);
            var root = await context.MediaFolders.FirstAsync(f => f.Slug == "landing-pages");
            var sub = await service.CreateFolderAsync("Remont na Banya", root.Id);

            Assert.NotNull(sub);
            Assert.Equal("Remont na Banya", sub.Name);
            Assert.Equal("remont-na-banya", sub.Slug);
            Assert.Equal("/landing-pages/remont-na-banya", sub.FullPath);
            Assert.Equal(root.Id, sub.ParentId);
        }
    }

    [Fact]
    public async Task RenameFolderAsync_CascadesPathUpdateToDescendants()
    {
        var dbName = $"UnifiedMediaDb_{Guid.NewGuid()}";
        var config = CreateConfiguration();
        var mockImageService = new Mock<IImageProcessingService>();

        Guid rootId;
        Guid subId;

        using (var context = CreateDbContext(dbName))
        {
            var service = new UnifiedMediaService(context, config, NullLogger<UnifiedMediaService>.Instance, mockImageService.Object);
            var root = await service.CreateFolderAsync("Old Root");
            var sub = await service.CreateFolderAsync("Child Folder", root.Id);
            rootId = root.Id;
            subId = sub.Id;
        }

        // Rename root
        using (var context = CreateDbContext(dbName))
        {
            var service = new UnifiedMediaService(context, config, NullLogger<UnifiedMediaService>.Instance, mockImageService.Object);
            var updated = await service.RenameFolderAsync(rootId, "New Root");
            Assert.Equal("/new-root", updated.FullPath);
        }

        // Verify child path was updated
        using (var context = CreateDbContext(dbName))
        {
            var child = await context.MediaFolders.FindAsync(subId);
            Assert.NotNull(child);
            Assert.Equal("/new-root/child-folder", child.FullPath);
        }
    }

    [Fact]
    public async Task EnsureFolderPathAsync_CreatesHierarchyIdempotently()
    {
        var dbName = $"UnifiedMediaDb_{Guid.NewGuid()}";
        var config = CreateConfiguration();
        var mockImageService = new Mock<IImageProcessingService>();

        using (var context = CreateDbContext(dbName))
        {
            var service = new UnifiedMediaService(context, config, NullLogger<UnifiedMediaService>.Instance, mockImageService.Object);
            var folder = await service.EnsureFolderPathAsync("/landing-pages/remont-na-apartament-sofia");

            Assert.NotNull(folder);
            Assert.Equal("/landing-pages/remont-na-apartament-sofia", folder.FullPath);
        }

        // Calling again should return the existing folder without creating duplicates
        using (var context = CreateDbContext(dbName))
        {
            var service = new UnifiedMediaService(context, config, NullLogger<UnifiedMediaService>.Instance, mockImageService.Object);
            var folder = await service.EnsureFolderPathAsync("/landing-pages/remont-na-apartament-sofia");

            Assert.NotNull(folder);
            var count = await context.MediaFolders.CountAsync();
            Assert.Equal(2, count); // "landing-pages" and "remont-na-apartament-sofia"
        }
    }

    [Fact]
    public async Task RegisterUploadedAssetAsync_SavesAsset_WithResolvedPublicUrl()
    {
        var dbName = $"UnifiedMediaDb_{Guid.NewGuid()}";
        var config = CreateConfiguration();
        var mockImageService = new Mock<IImageProcessingService>();

        Guid folderId;
        using (var context = CreateDbContext(dbName))
        {
            var service = new UnifiedMediaService(context, config, NullLogger<UnifiedMediaService>.Instance, mockImageService.Object);
            var folder = await service.CreateFolderAsync("Feed");
            folderId = folder.Id;

            var asset = await service.RegisterUploadedAssetAsync(
                folderId: folderId,
                r2Key: "feed/sample_video.mp4",
                fileName: "sample_video.mp4",
                contentType: "video/mp4",
                sizeBytes: 1048576,
                durationSeconds: 15.5);

            Assert.NotNull(asset);
            Assert.Equal("https://pub-test.r2.dev/feed/sample_video.mp4", asset.PublicUrl);
            Assert.Equal("video", asset.MediaType);
            Assert.Equal(1048576, asset.SizeBytes);
        }

        // Verify query in separate context
        using (var context = CreateDbContext(dbName))
        {
            var service = new UnifiedMediaService(context, config, NullLogger<UnifiedMediaService>.Instance, mockImageService.Object);
            var (items, totalCount) = await service.GetAssetsAsync(folderId, "video", null);

            Assert.Equal(1, totalCount);
            Assert.Single(items);
            Assert.Equal("sample_video.mp4", items[0].FileName);
        }
    }

    [Fact]
    public async Task MoveAndMetadataUpdate_WorksCorrectly()
    {
        var dbName = $"UnifiedMediaDb_{Guid.NewGuid()}";
        var config = CreateConfiguration();
        var mockImageService = new Mock<IImageProcessingService>();

        Guid assetId;
        Guid folderA;
        Guid folderB;

        using (var context = CreateDbContext(dbName))
        {
            var service = new UnifiedMediaService(context, config, NullLogger<UnifiedMediaService>.Instance, mockImageService.Object);
            var fA = await service.CreateFolderAsync("Folder A");
            var fB = await service.CreateFolderAsync("Folder B");
            folderA = fA.Id;
            folderB = fB.Id;

            var asset = await service.RegisterUploadedAssetAsync(
                folderId: folderA,
                r2Key: "folder-a/hero.webp",
                fileName: "hero.webp",
                contentType: "image/webp",
                sizeBytes: 50000);

            assetId = asset.Id;
        }

        // Move to folder B and update alt text
        using (var context = CreateDbContext(dbName))
        {
            var service = new UnifiedMediaService(context, config, NullLogger<UnifiedMediaService>.Instance, mockImageService.Object);
            await service.MoveAssetAsync(assetId, folderB);
            var updated = await service.UpdateAssetMetadataAsync(assetId, "Снимка Баня", "Bathroom Photo", "hero_renamed.webp", null);

            Assert.Equal(folderB, updated.FolderId);
            Assert.Equal("Снимка Баня", updated.AltTextBg);
            Assert.Equal("Bathroom Photo", updated.AltTextEn);
            Assert.Equal("hero_renamed.webp", updated.FileName);
        }
    }

    [Fact]
    public async Task SeedMediaFoldersAsync_SeedsDefaultSystemFolders()
    {
        var dbName = $"UnifiedMediaDb_{Guid.NewGuid()}";

        using (var context = CreateDbContext(dbName))
        {
            await context.SeedMediaFoldersAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var folders = await context.MediaFolders.ToListAsync();
            Assert.Contains(folders, f => f.Slug == "landing-pages" && f.IsSystem);
            Assert.Contains(folders, f => f.Slug == "feed" && f.IsSystem);
            Assert.Contains(folders, f => f.Slug == "categories" && f.IsSystem);
            Assert.Contains(folders, f => f.Slug == "portfolios" && f.IsSystem);
            Assert.Contains(folders, f => f.Slug == "general" && f.IsSystem);

            // Subfolders
            Assert.Contains(folders, f => f.FullPath == "/landing-pages/remont-na-apartament-sofia");
            Assert.Contains(folders, f => f.FullPath == "/landing-pages/remont-na-banya");
        }
    }
}
