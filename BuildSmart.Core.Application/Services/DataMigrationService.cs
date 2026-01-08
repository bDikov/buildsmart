using BuildSmart.Core.Application.Interfaces;

namespace BuildSmart.Core.Application.Services;

public class DataMigrationService
{
	private readonly IUnitOfWork _unitOfWork;

	public DataMigrationService(IUnitOfWork unitOfWork)
	{
		_unitOfWork = unitOfWork;
	}

	public virtual async Task<int> HashExistingPasswordsAsync()
	{
		var usersToUpdate = await _unitOfWork.Users.GetAllAsync();
		var updatedCount = 0;

		foreach (var user in usersToUpdate)
		{
			// Simple check to see if the password is likely already hashed.
			// BCrypt hashes start with a specific prefix, e.g., $2a$, $2b$, $2y$.
			if (user.HashedPassword != null)
			{
				user.HashedPassword = BCrypt.Net.BCrypt.HashPassword(user.HashedPassword);
				_unitOfWork.Users.Update(user);
				updatedCount++;
			}
		}

		if (updatedCount > 0)
		{
			await _unitOfWork.SaveChangesAsync();
		}

		return updatedCount;
	}
}