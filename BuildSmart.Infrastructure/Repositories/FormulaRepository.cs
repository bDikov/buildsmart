using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BuildSmart.Infrastructure.Repositories;

public class FormulaRepository : IFormulaRepository
{
    private readonly AppDbContext _context;

    public FormulaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Formula?> GetByIdAsync(Guid id)
    {
        return await _context.Formulas
            .Include(f => f.Questions)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<Formula?> GetByNameAsync(string name)
    {
        return await _context.Formulas
            .Include(f => f.Questions)
            .FirstOrDefaultAsync(f => f.Name == name);
    }

    public async Task<IEnumerable<Formula>> GetAllAsync()
    {
        return await _context.Formulas
            .Include(f => f.Questions)
            .ToListAsync();
    }

    public async Task AddAsync(Formula formula)
    {
        await _context.Formulas.AddAsync(formula);
    }

    public void Update(Formula formula)
    {
        _context.Formulas.Update(formula);
    }

    public void Delete(Formula formula)
    {
        _context.Formulas.Remove(formula);
    }
}
