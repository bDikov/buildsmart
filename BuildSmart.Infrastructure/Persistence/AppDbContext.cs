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
        public DbSet<JobPostFeedback> JobPostFeedbacks { get; set; } = null!;
	    public DbSet<Bid> Bids { get; set; } = null!;
	    public DbSet<TradesmanAuctionAction> TradesmanAuctionActions { get; set; } = null!;
	
		public DbSet<Booking> Bookings { get; set; } = null!;
	    public DbSet<ChangeOrder> ChangeOrders { get; set; } = null!;
		public DbSet<Review> Reviews { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<Certification> Certifications { get; set; } = null!;

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

    public async Task SeedTradesmanUser()
    {
        // 1. Ensure Painting Category exists
        var paintingCategory = await ServiceCategories.FirstOrDefaultAsync(c => c.Name == "Painting");
        if (paintingCategory == null)
        {
            paintingCategory = new ServiceCategory
            {
                Id = Guid.NewGuid(),
                Name = "Painting",
                Description = "Interior and exterior painting services",
                Status = CategoryStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await ServiceCategories.AddAsync(paintingCategory);
        }

        // 1.5 Ensure Electrical Category exists
        var electricalCategory = await ServiceCategories.FirstOrDefaultAsync(c => c.Name == "Electrical");
        if (electricalCategory == null)
        {
            electricalCategory = new ServiceCategory
            {
                Id = Guid.NewGuid(),
                Name = "Electrical",
                Description = "Electrical wiring and repair services",
                Status = CategoryStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await ServiceCategories.AddAsync(electricalCategory);
        }

        await SaveChangesAsync();

        // 2. Ensure Painter User exists
        if (!Users.Any(u => u.Email == "painter@buildsmart.com"))
        {
            var painterId = Guid.NewGuid();
            var painterUser = new User
            {
                Id = painterId,
                FirstName = "Paul",
                LastName = "Painter",
                Email = "painter@buildsmart.com",
                HashedPassword = BCrypt.Net.BCrypt.HashPassword("Painter123!"),
                Role = UserRoleTypes.Tradesman,
                Bio = "Specializing in high-end finishes.",
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var profile = new TradesmanProfile
            {
                Id = Guid.NewGuid(),
                UserId = painterId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            profile.Skills.Add(new TradesmanSkill
            {
                ServiceCategoryId = paintingCategory!.Id,
                VerificationStatus = SkillVerificationStatus.PortfolioVerified,
                YearsOfExperience = 5
            });

            painterUser.TradesmanProfile = profile;
            await Users.AddAsync(painterUser);
        }

        // 3. Ensure Electrician User exists
        if (!Users.Any(u => u.Email == "electrician@buildsmart.com"))
        {
            var sparkyId = Guid.NewGuid();
            var sparkyUser = new User
            {
                Id = sparkyId,
                FirstName = "Sam",
                LastName = "Sparky",
                Email = "electrician@buildsmart.com",
                HashedPassword = BCrypt.Net.BCrypt.HashPassword("Electrician123!"),
                Role = UserRoleTypes.Tradesman,
                Bio = "Certified master electrician.",
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var profile = new TradesmanProfile
            {
                Id = Guid.NewGuid(),
                UserId = sparkyId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            profile.Skills.Add(new TradesmanSkill
            {
                ServiceCategoryId = electricalCategory!.Id,
                VerificationStatus = SkillVerificationStatus.PortfolioVerified,
                YearsOfExperience = 10
            });

            sparkyUser.TradesmanProfile = profile;
            await Users.AddAsync(sparkyUser);
        }

        await SaveChangesAsync();
    }
}