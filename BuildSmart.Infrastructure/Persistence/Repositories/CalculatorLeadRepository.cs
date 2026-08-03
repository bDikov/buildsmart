using System.Threading.Tasks;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildSmart.Infrastructure.Persistence.Repositories;

public class CalculatorLeadRepository : ICalculatorLeadRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public CalculatorLeadRepository(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task AddLeadAsync(CalculatorLead lead)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.CalculatorLeads.Add(lead);
        await db.SaveChangesAsync();
    }
}
