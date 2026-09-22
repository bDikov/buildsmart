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
        await using var db = await CreateDbContextAsync();
        db.CalculatorLeads.Add(lead);
        await db.SaveChangesAsync();
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

        if (lead.MeetingDate.HasValue && lead.MeetingDate.Value.Kind != DateTimeKind.Utc)
        {
            lead.MeetingDate = DateTime.SpecifyKind(lead.MeetingDate.Value, DateTimeKind.Utc);
        }
        if (lead.FollowUpDate.HasValue && lead.FollowUpDate.Value.Kind != DateTimeKind.Utc)
        {
            lead.FollowUpDate = DateTime.SpecifyKind(lead.FollowUpDate.Value, DateTimeKind.Utc);
        }

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
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("does not exist") == true
                                        || ex.InnerException?.Message.Contains("42703") == true
                                        || ex.Message.Contains("does not exist"))
        {
            // The database table has not yet been migrated with new CRM columns.
            // Fallback: persist only the core columns and AdminNotes (which carries [CRM_META]).
            await using var fallbackDb = await CreateDbContextAsync();
            var fallbackLead = await fallbackDb.CalculatorLeads.FirstOrDefaultAsync(l => l.Id == lead.Id);
            if (fallbackLead != null)
            {
                fallbackLead.AdminNotes = lead.AdminNotes;
                fallbackLead.IsContacted = lead.IsContacted;
                fallbackLead.ContactedAt = lead.ContactedAt;
                fallbackLead.UpdatedAt = DateTime.UtcNow;
                await fallbackDb.SaveChangesAsync();
            }
        }
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

