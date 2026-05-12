using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace BuildSmart.Infrastructure.Persistence.Repositories;

public class AiCalculationRepository : IAiCalculationRepository
{
	private readonly AppDbContext _context;

	public AiCalculationRepository(AppDbContext context)
	{
		_context = context;
	}

	public async Task<AiCalculation?> GetByProjectAndCategoryAsync(Guid projectId, Guid serviceCategoryId)
	{
		return await _context.AiCalculations
			.Include(a => a.Tasks)
				.ThenInclude(t => t.SkuItems)
					.ThenInclude(s => s.ServiceSku)
			.Include(a => a.Tasks)
				.ThenInclude(t => t.AcceptanceCriteria)
			.FirstOrDefaultAsync(a => a.ProjectId == projectId && a.ServiceCategoryId == serviceCategoryId);
	}

	public async Task<IEnumerable<AiCalculation>> GetByProjectAsync(Guid projectId)
	{
		return await _context.AiCalculations
			.Where(a => a.ProjectId == projectId)
			.ToListAsync();
	}

	public async Task<IEnumerable<AiCalculation>> GetByProjectWithTasksAsync(Guid projectId)
	{
		return await _context.AiCalculations
			.Include(a => a.Tasks)
				.ThenInclude(t => t.AcceptanceCriteria)
			.Where(a => a.ProjectId == projectId)
			.ToListAsync();
	}

	public async Task AddAsync(AiCalculation calculation)
	{
		await _context.AiCalculations.AddAsync(calculation);
	}

	public void Update(AiCalculation calculation)
	{
		_context.AiCalculations.Update(calculation);
	}

	public void Delete(AiCalculation calculation)
	{
		_context.AiCalculations.Remove(calculation);
	}

	public void RemoveTasks(IEnumerable<AiCalculationTask> tasks)
	{
		_context.AiCalculationTasks.RemoveRange(tasks);
	}
}