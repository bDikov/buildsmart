using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildSmart.Infrastructure.Persistence.Repositories;

public class ServiceCategoryRepository : IServiceCategoryRepository
{
	private readonly AppDbContext _context;

	public ServiceCategoryRepository(AppDbContext context)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
	}

	public async Task<ServiceCategory?> GetByIdAsync(Guid id)
	{
		return await _context.ServiceCategories.FindAsync(id);
	}

	public async Task<IEnumerable<ServiceCategory>> GetAllAsync()
	{
		return await _context.ServiceCategories.ToListAsync();
	}

	public async Task AddAsync(ServiceCategory category)
	{
		await _context.ServiceCategories.AddAsync(category);
	}

	public void Update(ServiceCategory category)
	{
		_context.ServiceCategories.Update(category);
	}

	public async Task DeleteAsync(Guid id)
	{
		var category = await _context.ServiceCategories.FindAsync(id);
		if (category != null)
		{
			_context.ServiceCategories.Remove(category);
		}
	}

    public IQueryable<ServiceCategory> GetQueryable()
    {
        return _context.ServiceCategories.AsQueryable();
    }
}