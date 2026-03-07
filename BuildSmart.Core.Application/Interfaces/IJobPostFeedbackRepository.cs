using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.Interfaces;

public interface IJobPostFeedbackRepository
{
    Task<JobPostFeedback?> GetByIdAsync(Guid id);
    Task<IEnumerable<JobPostFeedback>> GetByJobPostIdAsync(Guid jobPostId);
    Task AddAsync(JobPostFeedback feedback);
    void Update(JobPostFeedback feedback);
    void Delete(JobPostFeedback feedback);
    IQueryable<JobPostFeedback> GetQueryable();
}