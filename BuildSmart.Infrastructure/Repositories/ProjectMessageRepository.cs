using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BuildSmart.Infrastructure.Repositories;

public class ProjectMessageRepository : IProjectMessageRepository
{
    private readonly AppDbContext _context;

    public ProjectMessageRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ProjectMessage>> GetMessagesPaginatedAsync(Guid projectId, int offset, int limit)
    {
        return await _context.ProjectMessages
            .Include(m => m.Sender)
            .Where(m => m.ProjectId == projectId)
            .OrderByDescending(m => m.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task AddAsync(ProjectMessage message)
    {
        await _context.ProjectMessages.AddAsync(message);
    }
}
