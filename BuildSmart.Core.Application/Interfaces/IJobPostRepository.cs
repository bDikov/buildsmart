using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.Interfaces;

public interface IJobPostRepository
{
    Task<JobPost?> GetByIdAsync(Guid id);
    Task<IEnumerable<JobPost>> GetJobsByProjectIdAsync(Guid projectId);
    Task AddAsync(JobPost jobPost);
    void Update(JobPost jobPost);
}
