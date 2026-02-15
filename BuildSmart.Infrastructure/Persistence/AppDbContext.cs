using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Entities.JoinEntities; // Added
using BuildSmart.Core.Domain.Enums; // Added for UserRoleTypes
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using BCrypt.Net; // Added for password hashing

namespace BuildSmart.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
	// Define DbSets only for your Aggregate Roots
	public DbSet<User> Users { get; set; } = null!;

    public DbSet<HomeownerProfile> HomeownerProfiles { get; set; } = null!;
    public DbSet<TradesmanSkill> TradesmanSkills { get; set; } = null!;
	public DbSet<ServiceCategory> ServiceCategories { get; set; } = null!;
	    public DbSet<TradesmanProfile> TradesmanProfiles { get; set; } = null!;
	    
	    public DbSet<Project> Projects { get; set; } = null!;
	    public DbSet<JobPost> JobPosts { get; set; } = null!;
	    public DbSet<JobPostQuestion> JobPostQuestions { get; set; } = null!;
	    public DbSet<Bid> Bids { get; set; } = null!;
	
		public DbSet<Booking> Bookings { get; set; } = null!;
	    public DbSet<ChangeOrder> ChangeOrders { get; set; } = null!;
		public DbSet<Review> Reviews { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!; // Added

	public AppDbContext(DbContextOptions<AppDbContext> options)
		: base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
	}

    public async Task SeedAdminUser()
    {
        if (!Users.Any(u => u.Role == UserRoleTypes.Admin))
        {
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@buildsmart.com",
                HashedPassword = BCrypt.Net.BCrypt.HashPassword("Admin123!"), // Default admin password
                Role = UserRoleTypes.Admin,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await Users.AddAsync(adminUser);
            await SaveChangesAsync();
        }
    }

    public async Task SeedHomeownerUser()
    {
        if (!Users.Any(u => u.Email == "homeowner@buildsmart.com"))
        {
            var homeownerId = Guid.NewGuid();
            var homeownerUser = new User
            {
                Id = homeownerId,
                FirstName = "Home",
                LastName = "Owner",
                Email = "homeowner@buildsmart.com",
                HashedPassword = BCrypt.Net.BCrypt.HashPassword("Homeowner123!"),
                Role = UserRoleTypes.Homeowner,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            homeownerUser.HomeownerProfile = new HomeownerProfile
            {
                Id = Guid.NewGuid(),
                UserId = homeownerId,
                Address = "123 Smart St"
            };

            await Users.AddAsync(homeownerUser);
            await SaveChangesAsync();
        }
    }
}