using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildSmart.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
	private readonly AppDbContext _context;

	public UserRepository(AppDbContext context)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
	}

	public async Task<IEnumerable<User>> GetAllAsync()
	{
		return await _context.Users.ToListAsync();
	}

	public async Task<User?> GetByIdAsync(Guid id)
	{
		return await _context.Users
            .Include(u => u.HomeownerProfile)
            .FirstOrDefaultAsync(u => u.Id == id);
	}

	public async Task<User?> GetByEmailAsync(string email)
	{
		return await _context.Users
			.FirstOrDefaultAsync(u => u.Email == email);
	}

	public async Task AddAsync(User user)
	{
		await _context.Users.AddAsync(user);
	}

	public void Update(User user)
	{
		_context.Users.Update(user);
	}

	    public void Delete(User user)
	    {
	        _context.Users.Remove(user);
	    }
	
	    public async Task<User?> GetByVerificationTokenAsync(string token)
	    {
	        return await _context.Users
	            .FirstOrDefaultAsync(u => u.EmailVerificationToken == token);
	    }
	}