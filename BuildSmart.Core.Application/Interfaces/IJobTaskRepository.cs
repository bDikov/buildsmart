using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.Interfaces;

public interface IJobTaskRepository
{
    IQueryable<JobTask> GetQueryable();
    Task<IEnumerable<JobTask>> GetTasksByJobPostAsync(Guid jobPostId);
    Task AddAsync(JobTask entity);
    Task AddRangeAsync(IEnumerable<JobTask> entities);
    void Delete(JobTask entity);
    void RemoveRange(IEnumerable<JobTask> entities);
}