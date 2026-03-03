using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildSmart.Infrastructure.Persistence.Repositories;

public class ProjectRepository : IProjectRepository
{
	private readonly AppDbContext _context;

	public ProjectRepository(AppDbContext context)
	{
		_context = context;
	}

	public async Task<Project?> GetByIdAsync(Guid id)
	{
		return await _context.Projects
			.Include(p => p.JobPosts)
				.ThenInclude(jp => jp.ServiceCategory)
			.Include(p => p.JobPosts)
				.ThenInclude(jp => jp.Feedbacks)
					.ThenInclude(f => f.Author)
			.Include(p => p.JobPosts)
				.ThenInclude(jp => jp.Feedbacks)
					.ThenInclude(f => f.Replies)
						.ThenInclude(r => r.Author)
			.Include(p => p.JobPosts)
				.ThenInclude(jp => jp.Questions)
					.ThenInclude(q => q.TradesmanProfile)
						.ThenInclude(tp => tp.User)
			.Include(p => p.JobPosts)
				.ThenInclude(jp => jp.Questions)
					.ThenInclude(q => q.Replies)
						.ThenInclude(r => r.Author)
			.FirstOrDefaultAsync(p => p.Id == id);
	}

	public async Task<IEnumerable<Project>> GetProjectsByHomeownerAsync(Guid homeownerId)
	{
		return await _context.Projects
				.AsSplitQuery()
				.Where(p => p.HomeownerId == homeownerId)
				.Include(p => p.JobPosts)
						.ThenInclude(jp => jp.ServiceCategory)
				.Include(p => p.JobPosts)
						.ThenInclude(jp => jp.Feedbacks)
								.ThenInclude(f => f.Author)
				.Include(p => p.JobPosts)
						.ThenInclude(jp => jp.Feedbacks)
								.ThenInclude(f => f.Replies)
										.ThenInclude(r => r.Author)
				.Include(p => p.JobPosts)
						.ThenInclude(jp => jp.Questions)
								.ThenInclude(q => q.TradesmanProfile)
										.ThenInclude(tp => tp.User)
				.Include(p => p.JobPosts)
						.ThenInclude(jp => jp.Questions)
								.ThenInclude(q => q.Author)
				.Include(p => p.JobPosts)
						.ThenInclude(jp => jp.Questions)
								.ThenInclude(q => q.Replies)
										.ThenInclude(r => r.TradesmanProfile)
												.ThenInclude(tp => tp.User)
				.Include(p => p.JobPosts)
						.ThenInclude(jp => jp.Questions)
								.ThenInclude(q => q.Replies)
										.ThenInclude(r => r.Author)
				.ToListAsync();
	}

	public async Task AddAsync(Project project)
	{
		await _context.Projects.AddAsync(project);
	}

	public void Update(Project project)
	{
		_context.Projects.Update(project);
	}

	public async Task DeleteAsync(Guid id)
	{
		var project = await _context.Projects.FindAsync(id);
		if (project != null)
		{
			_context.Projects.Remove(project);
		}
	}
}