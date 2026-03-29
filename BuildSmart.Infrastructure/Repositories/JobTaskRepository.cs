using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Infrastructure.Persistence.Repositories;

public class JobTaskRepository : IJobTaskRepository
{
    private readonly AppDbContext _context;

    public JobTaskRepository(AppDbContext context)
    {
        _context = context;
    }

    public IQueryable<JobTask> GetQueryable()
    {
        return _context.JobTasks.AsQueryable();
    }

    public async Task AddRangeAsync(IEnumerable<JobTask> entities)
    {
        await _context.JobTasks.AddRangeAsync(entities);
    }

    public void RemoveRange(IEnumerable<JobTask> entities)
    {
        _context.JobTasks.RemoveRange(entities);
    }
}