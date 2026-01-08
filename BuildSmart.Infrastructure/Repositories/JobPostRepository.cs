using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildSmart.Infrastructure.Persistence.Repositories;

public class JobPostRepository : IJobPostRepository
{
    private readonly AppDbContext _context;

    public JobPostRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<JobPost?> GetByIdAsync(Guid id)
    {
        return await _context.JobPosts
            .Include(jp => jp.Project)
            .Include(jp => jp.Bids)
            .FirstOrDefaultAsync(jp => jp.Id == id);
    }

    public async Task<IEnumerable<JobPost>> GetJobsByProjectIdAsync(Guid projectId)
    {
        return await _context.JobPosts
            .Where(jp => jp.ProjectId == projectId)
            .ToListAsync();
    }

    public async Task AddAsync(JobPost jobPost)
    {
        await _context.JobPosts.AddAsync(jobPost);
    }

    public void Update(JobPost jobPost)
    {
        _context.JobPosts.Update(jobPost);
    }
}
