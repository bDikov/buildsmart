using BuildSmart.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BuildSmart.Core.Application.Interfaces;

public interface IFormulaRepository
{
    Task<Formula?> GetByIdAsync(Guid id);
    Task<Formula?> GetByNameAsync(string name);
    Task<IEnumerable<Formula>> GetAllAsync();
    Task AddAsync(Formula formula);
    void Update(Formula formula);
    void Delete(Formula formula);
}
