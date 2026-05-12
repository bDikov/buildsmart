using BuildSmart.Core.Domain.Entities;
using System.Collections.Generic;

namespace BuildSmart.Core.Application.Interfaces;

public interface IAiCalculationRepository
{
    Task<AiCalculation?> GetByProjectAndCategoryAsync(Guid projectId, Guid serviceCategoryId);
    Task<IEnumerable<AiCalculation>> GetByProjectAsync(Guid projectId);
    Task<IEnumerable<AiCalculation>> GetByProjectWithTasksAsync(Guid projectId);
    Task AddAsync(AiCalculation calculation);
    void Update(AiCalculation calculation);
    void Delete(AiCalculation calculation);
    void RemoveTasks(IEnumerable<AiCalculationTask> tasks);
}