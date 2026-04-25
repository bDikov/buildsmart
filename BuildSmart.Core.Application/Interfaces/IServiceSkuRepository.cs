using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.Interfaces;

public interface IServiceSkuRepository
{
    Task<ServiceSku?> GetByIdAsync(Guid id);
    Task<IEnumerable<ServiceSku>> GetByCategoryAsync(Guid categoryId);
    Task AddAsync(ServiceSku sku);
    void Update(ServiceSku sku);
    void Delete(ServiceSku sku);
}
