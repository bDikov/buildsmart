using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BuildSmart.Infrastructure.Persistence.Repositories;

public class JobPostQuestionRepository : IJobPostQuestionRepository
{
    private readonly AppDbContext _context;

    public JobPostQuestionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<JobPostQuestion?> GetByIdAsync(Guid id)
    {
        return await _context.JobPostQuestions
            .Include(q => q.JobPost)
            .Include(q => q.TradesmanProfile)
                .ThenInclude(tp => tp.User)
            .Include(q => q.Author)
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    public async Task<IEnumerable<JobPostQuestion>> GetQuestionsByJobPostAsync(Guid jobPostId)
    {
        return await _context.JobPostQuestions
            .Include(q => q.TradesmanProfile)
                .ThenInclude(tp => tp.User)
            .Include(q => q.Author)
            .Include(q => q.Replies)
            .Where(q => q.JobPostId == jobPostId)
            .ToListAsync();
    }

    public async Task AddAsync(JobPostQuestion question)
    {
        await _context.JobPostQuestions.AddAsync(question);
    }

    public void Update(JobPostQuestion question)
    {
        _context.JobPostQuestions.Update(question);
    }

    public IQueryable<JobPostQuestion> GetQueryable() => _context.JobPostQuestions;
}
