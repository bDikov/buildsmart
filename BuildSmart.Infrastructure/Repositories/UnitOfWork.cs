using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Infrastructure.Persistence.Repositories; // We need this to find the concrete repositories

namespace BuildSmart.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork, IDisposable
{
	private readonly AppDbContext _context;

	public IUserRepository Users { get; }
	public IServiceCategoryRepository ServiceCategories { get; }
	public ITradesmanProfileRepository TradesmanProfiles { get; }
	public IBookingRepository Bookings { get; }
	public IReviewRepository Reviews { get; }
    public IProjectRepository Projects { get; }
    public IJobPostRepository JobPosts { get; }
    public IJobPostFeedbackRepository JobPostFeedbacks { get; }
    public IBidRepository Bids { get; }
    public INotificationRepository Notifications { get; }

	public UnitOfWork(AppDbContext context)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));

		// We initialize the concrete repositories here, passing them the context
		Users = new UserRepository(_context);
		ServiceCategories = new ServiceCategoryRepository(_context);
		TradesmanProfiles = new TradesmanProfileRepository(_context);
		Bookings = new BookingRepository(_context);
		Reviews = new ReviewRepository(_context);
        Projects = new ProjectRepository(_context);
        JobPosts = new JobPostRepository(_context);
        JobPostFeedbacks = new JobPostFeedbackRepository(_context);
        Bids = new BidRepository(_context);
        Notifications = new NotificationRepository(_context);
	}

	/// <summary>
	/// Saves all changes made in this unit of work to the database.
	/// </summary>
	/// <returns>The number of state entries written to the database.</returns>
	public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		return await _context.SaveChangesAsync(cancellationToken);
	}

	/// <summary>
	/// Disposes the database context.
	/// </summary>
	public void Dispose()
	{
		_context.Dispose();
		GC.SuppressFinalize(this);
	}
}