using BuildSmart.Core.Domain.Entities;
using BuildSmart.Infrastructure.Persistence;
using BuildSmart.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System.Text.Json;

namespace BuildSmart.Api.Tests;

public class CalculatorLeadChatTests
{
    private class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;
        public TestDbContextFactory(DbContextOptions<AppDbContext> options) => _options = options;
        public AppDbContext CreateDbContext() => new AppDbContext(_options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(new AppDbContext(_options));
    }

    private (CalculatorLeadRepository repo, DbContextOptions<AppDbContext> options) CreateRepository()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"CalculatorLeadChatDb_{Guid.NewGuid()}")
            .Options;
        var factory = new TestDbContextFactory(options);
        return (new CalculatorLeadRepository(factory), options);
    }

    [Fact]
    public async Task AddLeadAsync_And_UpdateLeadAsync_ShouldPersistChatAndEnforceUtcDates()
    {
        // Arrange
        var (repo, options) = CreateRepository();
        var leadId = Guid.NewGuid();
        var lead = new CalculatorLead
        {
            Id = leadId,
            Name = "Pesho",
            Email = "pesho@example.com",
            Phone = "0888123456",
            Scope = "full",
            SelectedArea = 85,
            BuildingStatus = "old",
            QualityTier = "standard",
            CreatedAt = DateTime.UtcNow,
            AdminNotes = "[CONFIG]:{\"scope\":\"full\"}\n[AI_CHAT_JSON]:[{\"timestamp\":\"2026-09-29 04:00\",\"sender\":\"user\",\"message\":\"Здравейте, интересува ме ремонт на апартамент.\"}]"
        };

        // Act
        await repo.AddLeadAsync(lead);

        // Assert - verify initial save in separate context
        using (var verifyDb = new AppDbContext(options))
        {
            var saved = await verifyDb.CalculatorLeads.FirstOrDefaultAsync(l => l.Id == leadId);
            Assert.NotNull(saved);
            Assert.Equal("Pesho", saved.Name);
            Assert.Contains("[AI_CHAT_JSON]:", saved.AdminNotes);
            Assert.Equal(DateTimeKind.Utc, saved.CreatedAt.Kind);
        }

        // Update with consultant response
        lead.AdminNotes += "\n[LIVE_TAKEOVER]:true";
        lead.UpdatedAt = DateTime.UtcNow;
        await repo.UpdateLeadAsync(lead);

        using (var verifyDb = new AppDbContext(options))
        {
            var updated = await verifyDb.CalculatorLeads.FirstOrDefaultAsync(l => l.Id == leadId);
            Assert.NotNull(updated);
            Assert.Contains("[LIVE_TAKEOVER]:true", updated.AdminNotes);
            Assert.Equal(DateTimeKind.Utc, updated.UpdatedAt.Kind);
        }
    }

    [Fact]
    public async Task UpdateLeadAsync_WithLargeChat_ShouldPruneWithoutBreakingJson()
    {
        // Arrange
        var (repo, options) = CreateRepository();
        var leadId = Guid.NewGuid();
        
        // Build a chat list with 40 long messages that will exceed 4000 chars
        var chatList = new List<object>();
        for (int i = 0; i < 40; i++)
        {
            chatList.Add(new
            {
                timestamp = $"2026-09-29 04:{i:D2}",
                sender = i % 2 == 0 ? "user" : "Бончо Диков (Консултант)",
                message = $"Това е подробно съобщение номер {i} с много конкретни строителни детайли, квадратури, изолации и лазерни измервания за обекта в София."
            });
        }

        var fullJson = JsonSerializer.Serialize(chatList);
        var notes = $"[CONFIG]:{{\"scope\":\"full\"}}\n[AI_CHAT_JSON]:{fullJson}";
        Assert.True(notes.Length > 4000);

        var lead = new CalculatorLead
        {
            Id = leadId,
            Name = "Pesho Large",
            Email = "large@example.com",
            Phone = "0888999888",
            CreatedAt = DateTime.UtcNow,
            AdminNotes = notes
        };

        // Act
        await repo.AddLeadAsync(lead);

        // Assert
        using (var verifyDb = new AppDbContext(options))
        {
            var saved = await verifyDb.CalculatorLeads.FirstOrDefaultAsync(l => l.Id == leadId);
            Assert.NotNull(saved);
            Assert.NotNull(saved.AdminNotes);
            Assert.True(saved.AdminNotes.Length <= 3900);

            // Verify the JSON inside AdminNotes is still completely valid and parseable
            const string marker = "[AI_CHAT_JSON]:";
            int idx = saved.AdminNotes.IndexOf(marker);
            Assert.True(idx >= 0);
            string jsonPart = saved.AdminNotes.Substring(idx + marker.Length);
            
            using var doc = JsonDocument.Parse(jsonPart);
            Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
            Assert.True(doc.RootElement.GetArrayLength() > 0);

            // Verify newest message was kept
            var lastElement = doc.RootElement[doc.RootElement.GetArrayLength() - 1];
            Assert.Contains("39", lastElement.GetProperty("message").GetString());
        }
    }
}
