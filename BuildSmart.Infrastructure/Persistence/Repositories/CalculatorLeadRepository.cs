using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildSmart.Infrastructure.Persistence.Repositories;

public class CalculatorLeadRepository : ICalculatorLeadRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly AppDbContext? _db;

    public CalculatorLeadRepository(IDbContextFactory<AppDbContext> dbFactory, AppDbContext? db = null)
    {
        _dbFactory = dbFactory;
        _db = db;
    }

    private async Task<AppDbContext> CreateDbContextAsync()
    {
        if (_dbFactory != null)
        {
            return await _dbFactory.CreateDbContextAsync();
        }
        return _db!;
    }

    public async Task AddLeadAsync(CalculatorLead lead)
    {
        if (lead.CreatedAt == default)
        {
            lead.CreatedAt = DateTime.UtcNow;
        }
        else if (lead.CreatedAt.Kind != DateTimeKind.Utc)
        {
            lead.CreatedAt = DateTime.SpecifyKind(lead.CreatedAt, DateTimeKind.Utc);
        }
        if (lead.UpdatedAt == default)
        {
            lead.UpdatedAt = DateTime.UtcNow;
        }
        else if (lead.UpdatedAt.Kind != DateTimeKind.Utc)
        {
            lead.UpdatedAt = DateTime.SpecifyKind(lead.UpdatedAt, DateTimeKind.Utc);
        }

        if (lead.MeetingDate.HasValue && lead.MeetingDate.Value.Kind != DateTimeKind.Utc)
        {
            lead.MeetingDate = DateTime.SpecifyKind(lead.MeetingDate.Value, DateTimeKind.Utc);
        }
        if (lead.FollowUpDate.HasValue && lead.FollowUpDate.Value.Kind != DateTimeKind.Utc)
        {
            lead.FollowUpDate = DateTime.SpecifyKind(lead.FollowUpDate.Value, DateTimeKind.Utc);
        }
        if (lead.ContactedAt.HasValue && lead.ContactedAt.Value.Kind != DateTimeKind.Utc)
        {
            lead.ContactedAt = DateTime.SpecifyKind(lead.ContactedAt.Value, DateTimeKind.Utc);
        }

        lead.AdminNotes = SafeTruncateNotes(lead.AdminNotes);

        await using var db = await CreateDbContextAsync();
        db.CalculatorLeads.Add(lead);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CalculatorLeadRepository] AddLeadAsync EF failed: {ex.Message}. Attempting raw SQL fallback...");
            // Fallback: safely insert only the legacy/core columns via raw SQL command
            try
            {
                await using var fallbackDb = await CreateDbContextAsync();
                var sql = @"
                    INSERT INTO ""CalculatorLeads"" (
                        ""Id"", ""Email"", ""Phone"", ""Name"", ""Scope"", ""SelectedArea"", 
                        ""BuildingStatus"", ""QualityTier"", ""IncludeFurniture"", ""IncludeEquipment"", 
                        ""BathroomCount"", ""MinPriceEur"", ""MaxPriceEur"", ""MinPriceBgn"", ""MaxPriceBgn"", 
                        ""EstimatedDays"", ""IsEmailVerified"", ""VerificationStatus"", ""VerificationReason"", 
                        ""UtmSource"", ""UtmMedium"", ""UtmCampaign"", ""UtmTerm"", ""UtmContent"", 
                        ""IsContacted"", ""ContactedAt"", ""AdminNotes"", ""CreatedAt"", ""UpdatedAt""
                    ) VALUES (
                        {0}, {1}, {2}, {3}, {4}, {5}, 
                        {6}, {7}, {8}, {9}, 
                        {10}, {11}, {12}, {13}, {14}, 
                        {15}, {16}, {17}, {18}, 
                        {19}, {20}, {21}, {22}, {23}, 
                        {24}, {25}, {26}, {27}, {28}
                    )";
                object?[] sqlParams = new object?[]
                {
                    lead.Id, lead.Email, lead.Phone, lead.Name, lead.Scope, lead.SelectedArea,
                    lead.BuildingStatus, lead.QualityTier, lead.IncludeFurniture, lead.IncludeEquipment,
                    lead.BathroomCount, lead.MinPriceEur, lead.MaxPriceEur, lead.MinPriceBgn, lead.MaxPriceBgn,
                    lead.EstimatedDays, lead.IsEmailVerified, lead.VerificationStatus, lead.VerificationReason,
                    lead.UtmSource, lead.UtmMedium, lead.UtmCampaign, lead.UtmTerm, lead.UtmContent,
                    lead.IsContacted, lead.ContactedAt, lead.AdminNotes, lead.CreatedAt, lead.UpdatedAt
                };
                await fallbackDb.Database.ExecuteSqlRawAsync(sql, sqlParams!);
            }
            catch (Exception fallbackEx)
            {
                Console.WriteLine($"[CalculatorLeadRepository] AddLeadAsync raw SQL fallback failed: {fallbackEx.Message}");
            }
        }
    }

    public async Task<CalculatorLead?> GetByIdAsync(Guid id)
    {
        await using var db = await CreateDbContextAsync();
        return await db.CalculatorLeads.FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<List<CalculatorLead>> GetLeadsAsync()
    {
        await using var db = await CreateDbContextAsync();
        return await db.CalculatorLeads
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    public async Task UpdateLeadAsync(CalculatorLead lead)
    {
        lead.UpdatedAt = DateTime.UtcNow;

        if (lead.CreatedAt == default)
        {
            lead.CreatedAt = DateTime.UtcNow;
        }
        else if (lead.CreatedAt.Kind != DateTimeKind.Utc)
        {
            lead.CreatedAt = DateTime.SpecifyKind(lead.CreatedAt, DateTimeKind.Utc);
        }

        if (lead.MeetingDate.HasValue && lead.MeetingDate.Value.Kind != DateTimeKind.Utc)
        {
            lead.MeetingDate = DateTime.SpecifyKind(lead.MeetingDate.Value, DateTimeKind.Utc);
        }
        if (lead.FollowUpDate.HasValue && lead.FollowUpDate.Value.Kind != DateTimeKind.Utc)
        {
            lead.FollowUpDate = DateTime.SpecifyKind(lead.FollowUpDate.Value, DateTimeKind.Utc);
        }
        if (lead.ContactedAt.HasValue && lead.ContactedAt.Value.Kind != DateTimeKind.Utc)
        {
            lead.ContactedAt = DateTime.SpecifyKind(lead.ContactedAt.Value, DateTimeKind.Utc);
        }

        lead.AdminNotes = SafeTruncateNotes(lead.AdminNotes);

        await using var db = await CreateDbContextAsync();
        var existing = await db.CalculatorLeads.FirstOrDefaultAsync(l => l.Id == lead.Id);
        if (existing != null)
        {
            db.Entry(existing).CurrentValues.SetValues(lead);
        }
        else
        {
            db.CalculatorLeads.Update(lead);
        }

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CalculatorLeadRepository] UpdateLeadAsync EF failed: {ex.Message}. Attempting raw SQL fallback...");
            try
            {
                await using var fallbackDb = await CreateDbContextAsync();
                var updateSql = @"
                    UPDATE ""CalculatorLeads"" SET 
                        ""AdminNotes"" = {0},
                        ""IsContacted"" = {1},
                        ""ContactedAt"" = {2},
                        ""UpdatedAt"" = {3}
                    WHERE ""Id"" = {4}";
                object?[] updateParams = new object?[] { lead.AdminNotes, lead.IsContacted, lead.ContactedAt, DateTime.UtcNow, lead.Id };
                await fallbackDb.Database.ExecuteSqlRawAsync(updateSql, updateParams!);
            }
            catch (Exception fallbackEx)
            {
                Console.WriteLine($"[CalculatorLeadRepository] Raw SQL fallback also failed: {fallbackEx.Message}");
            }
        }
    }

    private static string? SafeTruncateNotes(string? notes, int maxLength = 3900)
    {
        if (string.IsNullOrEmpty(notes) || notes.Length <= maxLength) return notes;

        const string marker = "[AI_CHAT_JSON]:";
        int idx = notes.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            try
            {
                var before = notes.Substring(0, idx + marker.Length);
                var jsonPart = notes.Substring(idx + marker.Length);
                int endOfJson = jsonPart.IndexOf("\n[");
                string arrayStr = endOfJson >= 0 ? jsonPart.Substring(0, endOfJson) : jsonPart;
                string after = endOfJson >= 0 ? jsonPart.Substring(endOfJson) : string.Empty;

                var items = System.Text.Json.JsonSerializer.Deserialize<List<System.Text.Json.Nodes.JsonObject>>(arrayStr);
                if (items != null && items.Count > 1)
                {
                    while (items.Count > 1 && (before.Length + System.Text.Json.JsonSerializer.Serialize(items).Length + after.Length) > maxLength)
                    {
                        items.RemoveAt(0);
                    }
                    var updated = before + System.Text.Json.JsonSerializer.Serialize(items) + after;
                    if (updated.Length <= maxLength) return updated;
                }
            }
            catch
            {
                // Fallback to substring
            }
        }

        return notes.Substring(0, maxLength);
    }

    public async Task DeleteLeadAsync(Guid id)
    {
        await using var db = await CreateDbContextAsync();
        var lead = await db.CalculatorLeads.FirstOrDefaultAsync(l => l.Id == id);
        if (lead != null)
        {
            db.CalculatorLeads.Remove(lead);
            await db.SaveChangesAsync();
        }
    }

    public IQueryable<CalculatorLead> GetQueryable()
    {
        if (_db != null)
        {
            return _db.CalculatorLeads;
        }

        return _dbFactory.CreateDbContext().CalculatorLeads;
    }
}

