using BuildSmart.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BuildSmart.Core.Application.Interfaces;

public interface IProjectMessageRepository
{
    Task<IEnumerable<ProjectMessage>> GetMessagesPaginatedAsync(Guid projectId, int offset, int limit);
    Task AddAsync(ProjectMessage message);
}
