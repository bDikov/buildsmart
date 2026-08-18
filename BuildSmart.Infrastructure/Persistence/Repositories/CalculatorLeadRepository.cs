using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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

    private static async Task EnsureTableCreatedAsync(AppDbContext db)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""CalculatorLeads"" (
                    ""Id"" uuid NOT NULL CONSTRAINT ""PK_CalculatorLeads"" PRIMARY KEY,
                    ""Email"" text NOT NULL,
                    ""Phone"" text NULL,
                    ""Name"" text NULL,
                    ""Scope"" text NOT NULL,
                    ""SelectedArea"" integer NOT NULL,
                    ""BuildingStatus"" text NOT NULL,
                    ""QualityTier"" text NOT NULL,
                    ""IncludeFurniture"" boolean NOT NULL,
                    ""IncludeEquipment"" boolean NOT NULL,
                    ""BathroomCount"" integer NOT NULL,
                    ""MinPriceEur"" numeric NOT NULL,
                    ""MaxPriceEur"" numeric NOT NULL,
                    ""MinPriceBgn"" numeric NOT NULL,
                    ""MaxPriceBgn"" numeric NOT NULL,
                    ""EstimatedDays"" integer NOT NULL,
                    ""IsEmailVerified"" boolean NOT NULL DEFAULT false,
                    ""VerificationStatus"" text NOT NULL DEFAULT 'Valid',
                    ""VerificationReason"" text NULL,
                    ""UtmSource"" text NULL,
                    ""UtmMedium"" text NULL,
                    ""UtmCampaign"" text NULL,
                    ""UtmTerm"" text NULL,
                    ""UtmContent"" text NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
                );
            ");
        }
        catch { }
    }

    public async Task AddLeadAsync(CalculatorLead lead)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        try
        {
            db.CalculatorLeads.Add(lead);
            await db.SaveChangesAsync();
        }
        catch
        {
            await EnsureTableCreatedAsync(db);
            db.CalculatorLeads.Add(lead);
            await db.SaveChangesAsync();
        }
    }

    public async Task<CalculatorLead?> GetByIdAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        try
        {
            return await db.CalculatorLeads.FirstOrDefaultAsync(l => l.Id == id);
        }
        catch
        {
            await EnsureTableCreatedAsync(db);
            return await db.CalculatorLeads.FirstOrDefaultAsync(l => l.Id == id);
        }
    }

    public async Task<List<CalculatorLead>> GetLeadsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        try
        {
            return await db.CalculatorLeads
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }
        catch
        {
            await EnsureTableCreatedAsync(db);
            return await db.CalculatorLeads
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
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

