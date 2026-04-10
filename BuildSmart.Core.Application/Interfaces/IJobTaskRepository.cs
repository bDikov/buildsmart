using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.Interfaces;

public interface IJobTaskRepository
{
    IQueryable<JobTask> GetQueryable();
    Task AddRangeAsync(IEnumerable<JobTask> entities);
    void RemoveRange(IEnumerable<JobTask> entities);
}