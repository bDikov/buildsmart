using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.Interfaces;

public interface ICertificationRepository
{
    Task<Certification?> GetByIdAsync(Guid id);
    Task<IEnumerable<Certification>> GetByTradesmanProfileIdAsync(Guid tradesmanProfileId);
    Task AddAsync(Certification certification);
    void Update(Certification certification);
    void Delete(Certification certification);
}
