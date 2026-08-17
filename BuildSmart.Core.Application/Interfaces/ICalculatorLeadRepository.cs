using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.Interfaces;

public interface ICalculatorLeadRepository
{
    Task AddLeadAsync(CalculatorLead lead);
    Task<CalculatorLead?> GetByIdAsync(Guid id);
    Task<List<CalculatorLead>> GetLeadsAsync();
    IQueryable<CalculatorLead> GetQueryable();
}

