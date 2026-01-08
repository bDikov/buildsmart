using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.Interfaces;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id);
    Task<IEnumerable<Project>> GetProjectsByHomeownerAsync(Guid homeownerId);
    Task AddAsync(Project project);
    void Update(Project project);
}
