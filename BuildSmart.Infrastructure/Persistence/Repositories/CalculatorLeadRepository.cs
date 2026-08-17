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

    public async Task AddLeadAsync(CalculatorLead lead)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.CalculatorLeads.Add(lead);
        await db.SaveChangesAsync();
    }

    public async Task<CalculatorLead?> GetByIdAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.CalculatorLeads.FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<List<CalculatorLead>> GetLeadsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.CalculatorLeads
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
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

