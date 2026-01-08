using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.Interfaces;

/// <summary>
/// Defines the contract for a repository handling User data operations.
/// This interface is part of the Application layer and will be implemented in the Infrastructure layer.
/// </summary>
public interface IUserRepository
{
	Task<IEnumerable<User>> GetAllAsync();

	Task<User?> GetByIdAsync(Guid id);

	Task<User?> GetByEmailAsync(string email);

	Task AddAsync(User user);

	void Update(User user);

	void Delete(User user);

	Task<User?> GetByVerificationTokenAsync(string token);
}