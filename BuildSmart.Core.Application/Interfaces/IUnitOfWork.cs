namespace BuildSmart.Core.Application.Interfaces;

/// <summary>
/// Defines the contract for a Unit of Work,
/// which manages all repositories and database transactions.
/// </summary>
public interface IUnitOfWork
{
	/// <summary>
	/// Gets the repository for User operations.
	/// </summary>
	IUserRepository Users { get; }

	/// <summary>
	/// Gets the repository for Booking operations.
	/// </summary>
	IBookingRepository Bookings { get; }

	/// <summary>
	/// Gets the repository for TradesmanProfile operations.
	/// </summary>
	ITradesmanProfileRepository TradesmanProfiles { get; }

	/// <summary>
	/// Gets the repository for Review operations.
	/// </summary>
	IReviewRepository Reviews { get; }

	/// <summary>
	/// Gets the repository for ServiceCategory operations.
	/// </summary>
	IServiceCategoryRepository ServiceCategories { get; }

    IProjectRepository Projects { get; }
    IJobPostRepository JobPosts { get; }
    IBidRepository Bids { get; }

	/// <summary>
	/// Saves all changes made in this unit of work to the underlying database
	/// as a single transaction.
	/// </summary>
	/// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
	/// <returns>The number of state entries written to the database.</returns>
	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}