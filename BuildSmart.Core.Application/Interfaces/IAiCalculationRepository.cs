using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.Interfaces;

public interface IAiCalculationRepository
{
    Task<AiCalculation?> GetByProjectAndCategoryAsync(Guid projectId, Guid serviceCategoryId);
    Task AddAsync(AiCalculation calculation);
    void Update(AiCalculation calculation);
    void Delete(AiCalculation calculation);
    void RemoveTasks(IEnumerable<AiCalculationTask> tasks);
}