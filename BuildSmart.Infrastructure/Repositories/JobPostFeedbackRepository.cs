using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BuildSmart.Infrastructure.Persistence.Repositories;

public class JobPostFeedbackRepository : IJobPostFeedbackRepository
{
    private readonly AppDbContext _context;

    public JobPostFeedbackRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<JobPostFeedback?> GetByIdAsync(Guid id)
    {
        return await _context.JobPostFeedbacks
            .Include(f => f.Author)
            .Include(f => f.Replies)
                .ThenInclude(r => r.Author)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<IEnumerable<JobPostFeedback>> GetByJobPostIdAsync(Guid jobPostId)
    {
        return await _context.JobPostFeedbacks
            .Where(f => f.JobPostId == jobPostId && f.ParentFeedbackId == null)
            .Include(f => f.Author)
            .Include(f => f.Replies)
                .ThenInclude(r => r.Author)
            .OrderBy(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(JobPostFeedback feedback)
    {
        await _context.JobPostFeedbacks.AddAsync(feedback);
    }

    public void Update(JobPostFeedback feedback)
    {
        _context.JobPostFeedbacks.Update(feedback);
    }

    public void Delete(JobPostFeedback feedback)
    {
        _context.JobPostFeedbacks.Remove(feedback);
    }
}