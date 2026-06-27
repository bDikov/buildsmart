using BuildSmart.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BuildSmart.Core.Application.Interfaces;

public interface IQuestionRepository
{
    Task<Question?> GetByIdAsync(Guid id);
    Task<Question?> GetByCodeAsync(string questionCode);
    Task<IEnumerable<Question>> GetAllAsync();
    Task AddAsync(Question question);
    void Update(Question question);
    void Delete(Question question);
}
