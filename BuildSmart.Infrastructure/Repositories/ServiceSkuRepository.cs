using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BuildSmart.Infrastructure.Repositories;

public class ServiceSkuRepository : IServiceSkuRepository
{
    private readonly AppDbContext _context;

    public ServiceSkuRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceSku?> GetByIdAsync(Guid id)
    {
        return await _context.ServiceSkus.FindAsync(id);
    }

    public async Task<IEnumerable<ServiceSku>> GetByCategoryAsync(Guid categoryId)
    {
        return await _context.ServiceSkus
            .Where(s => s.ServiceCategoryId == categoryId)
            .ToListAsync();
    }

    public async Task AddAsync(ServiceSku sku)
    {
        await _context.ServiceSkus.AddAsync(sku);
    }

    public void Update(ServiceSku sku)
    {
        _context.ServiceSkus.Update(sku);
    }

    public void Delete(ServiceSku sku)
    {
        _context.ServiceSkus.Remove(sku);
    }
}
