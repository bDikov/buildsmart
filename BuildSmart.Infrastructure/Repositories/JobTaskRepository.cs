using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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

    public async Task<IEnumerable<JobTask>> GetTasksByJobPostAsync(Guid jobPostId)
    {
        return await _context.JobTasks
            .Where(t => t.JobPostId == jobPostId)
            .ToListAsync();
    }

    public async Task AddAsync(JobTask entity)
    {
        await _context.JobTasks.AddAsync(entity);
    }

    public async Task AddRangeAsync(IEnumerable<JobTask> entities)
    {
        await _context.JobTasks.AddRangeAsync(entities);
    }

    public void Delete(JobTask entity)
    {
        _context.JobTasks.Remove(entity);
    }

    public void RemoveRange(IEnumerable<JobTask> entities)
    {
        _context.JobTasks.RemoveRange(entities);
    }
}