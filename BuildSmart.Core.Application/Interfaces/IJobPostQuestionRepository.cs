using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.Interfaces;

public interface IJobPostQuestionRepository
{
    Task<JobPostQuestion?> GetByIdAsync(Guid id);
    Task<IEnumerable<JobPostQuestion>> GetQuestionsByJobPostAsync(Guid jobPostId);
    Task AddAsync(JobPostQuestion question);
    void Update(JobPostQuestion question);
}
