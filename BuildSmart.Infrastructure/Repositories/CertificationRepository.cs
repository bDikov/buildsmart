using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildSmart.Infrastructure.Persistence.Repositories;

public class CertificationRepository : ICertificationRepository
{
    private readonly AppDbContext _context;

    public CertificationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Certification?> GetByIdAsync(Guid id)
    {
        return await _context.Certifications
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Certification>> GetByTradesmanProfileIdAsync(Guid tradesmanProfileId)
    {
        return await _context.Certifications
            .Where(c => c.TradesmanProfileId == tradesmanProfileId)
            .ToListAsync();
    }

    public async Task AddAsync(Certification certification)
    {
        await _context.Certifications.AddAsync(certification);
    }

    public void Update(Certification certification)
    {
        _context.Certifications.Update(certification);
    }

    public void Delete(Certification certification)
    {
        _context.Certifications.Remove(certification);
    }
}
