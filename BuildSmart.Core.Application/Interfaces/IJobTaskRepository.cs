using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.Interfaces;

public interface IJobTaskRepository
{
    IQueryable<JobTask> GetQueryable();
    Task<IEnumerable<JobTask>> GetTasksByJobPostAsync(Guid jobPostId);
    Task AddAsync(JobTask entity);
    Task AddRangeAsync(IEnumerable<JobTask> entities);
    void Update(JobTask entity);
    void Delete(JobTask entity);
    void RemoveRange(IEnumerable<JobTask> entities);
    
    void RemoveSkuItems(IEnumerable<TaskSkuItem> skuItems);
    void RemoveAcceptanceCriteria(IEnumerable<TaskAcceptanceCriteria> criteria);
}