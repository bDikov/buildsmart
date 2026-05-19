using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Entities.JoinEntities;
using BuildSmart.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BuildSmart.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
	// Define DbSets only for your Aggregate Roots
	public DbSet<User> Users { get; set; } = null!;
    public DbSet<HomeownerProfile> HomeownerProfiles { get; set; } = null!;
	public DbSet<TradesmanSkill> TradesmanSkills { get; set; } = null!;
	public DbSet<ServiceCategory> ServiceCategories { get; set; } = null!;
	public DbSet<ServiceCategoryTranslation> ServiceCategoryTranslations { get; set; } = null!;
	public DbSet<ServiceSku> ServiceSkus { get; set; } = null!;
	public DbSet<ServiceSkuTranslation> ServiceSkuTranslations { get; set; } = null!;
	public DbSet<TradesmanProfile> TradesmanProfiles { get; set; } = null!;
	public DbSet<Project> Projects { get; set; } = null!;
	public DbSet<JobPost> JobPosts { get; set; } = null!;
	public DbSet<JobPostQuestion> JobPostQuestions { get; set; } = null!;
    public DbSet<JobPostFeedback> JobPostFeedbacks { get; set; } = null!;
	public DbSet<Bid> Bids { get; set; } = null!;
	public DbSet<JobTask> JobTasks { get; set; } = null!;
	public DbSet<TaskSkuItem> TaskSkuItems { get; set; } = null!;
	public DbSet<TaskAcceptanceCriteria> TaskAcceptanceCriteria { get; set; } = null!;
    
    // AI Calculations
    public DbSet<AiCalculation> AiCalculations { get; set; } = null!;
    public DbSet<AiCalculationTask> AiCalculationTasks { get; set; } = null!;
    public DbSet<AiCalculationSkuItem> AiCalculationSkuItems { get; set; } = null!;
    public DbSet<AiCalculationCriteria> AiCalculationCriteria { get; set; } = null!;

	public DbSet<BidItem> BidItems { get; set; } = null!;
	public DbSet<TradesmanAuctionAction> TradesmanAuctionActions { get; set; } = null!;
	public DbSet<Booking> Bookings { get; set; } = null!;
	public DbSet<MilestonePayment> MilestonePayments { get; set; } = null!;
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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w => w.Ignore(
            RelationalEventId.PendingModelChangesWarning,
            CoreEventId.NavigationBaseIncludeIgnored));
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
                HashedPassword = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
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
        var paintingCategory = await ServiceCategories.FirstOrDefaultAsync(c => c.Name == "Бояджийски и шпакловъчни услуги (Painting)");
        if (paintingCategory == null)
        {
            paintingCategory = new ServiceCategory
            {
                Id = Guid.NewGuid(),
                Name = "Бояджийски и шпакловъчни услуги (Painting)",
                Description = "Interior and exterior painting services",
                Status = CategoryStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await ServiceCategories.AddAsync(paintingCategory);
        }

        var electricalCategory = await ServiceCategories.FirstOrDefaultAsync(c => c.Name == "Електрическа Инсталация");
        if (electricalCategory == null)
        {
            electricalCategory = new ServiceCategory
            {
                Id = Guid.NewGuid(),
                Name = "Електрическа Инсталация",
                Description = "Electrical wiring and repair services",
                Status = CategoryStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await ServiceCategories.AddAsync(electricalCategory);
        }

        await SaveChangesAsync();

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

    public async Task SeedCategoriesAndQuestionsAsync()
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "Categories_Seed_Templates.json");
        if (!System.IO.File.Exists(filePath))
        {
            return; // Skip if file not found
        }

        var json = await System.IO.File.ReadAllTextAsync(filePath);
        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var seedData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, CategorySeedDto>>(json, options);
        
        if (seedData == null) return;

        foreach (var kvp in seedData)
        {
            var categoryName = kvp.Value.Name;
            var isGlobal = kvp.Key == "global_category";

            var category = await ServiceCategories.FirstOrDefaultAsync(c => c.Name == categoryName);
            if (category == null)
            {
                category = new ServiceCategory
                {
                    Id = Guid.NewGuid(),
                    Name = categoryName,
                    Status = CategoryStatus.Active,
                    IsGlobal = isGlobal,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await ServiceCategories.AddAsync(category);
            }
            else
            {
                category.IsGlobal = isGlobal;
            }
            
            // Always update the template structure to match the latest JSON
            category.TemplateStructure = System.Text.Json.JsonSerializer.Serialize(kvp.Value.TemplateStructure);
        }

        await SaveChangesAsync();
    }

    public async Task SeedSkusAsync()
    {
        // 1. Seed from MarketData_Sofia_Seed.json (General Market Data)
        var marketDataPath = Path.Combine(AppContext.BaseDirectory, "MarketData_Sofia_Seed.json");
        if (System.IO.File.Exists(marketDataPath))
        {
            var json = await System.IO.File.ReadAllTextAsync(marketDataPath);
            var marketData = System.Text.Json.JsonSerializer.Deserialize<List<MarketCategorySeedDto>>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (marketData != null)
            {
                foreach (var marketCat in marketData)
                {
                    // Map market category names to our DB category names
                    var dbCategoryName = MapMarketCategoryToDbName(marketCat.Category);
                    var category = await ServiceCategories.FirstOrDefaultAsync(c => c.Name == dbCategoryName);
                    
                    if (category != null)
                    {
                        var prefix = GetCategoryPrefix(dbCategoryName);
                        int count = 1;
                        foreach (var marketTask in marketCat.Tasks)
                        {
                            var skuCode = $"{prefix}-{count:D3}";
                            var existingSku = await ServiceSkus.Include(s => s.Translations).FirstOrDefaultAsync(s => s.SkuCode == skuCode);
                            
                            if (existingSku == null)
                            {
                                var skuId = Guid.NewGuid();
                                var newSku = new ServiceSku
                                {
                                    Id = skuId,
                                    ServiceCategoryId = category.Id,
                                    SkuCode = skuCode,
                                    Name = marketTask.Name,
                                    Description = $"{marketTask.Name} ({marketTask.Unit})",
                                    BasePrice = marketTask.MaxPrice,
                                    UnitType = MapMarketUnitToUnitType(marketTask.Unit),
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                };

                                newSku.Translations.Add(new ServiceSkuTranslation
                                {
                                    Id = Guid.NewGuid(),
                                    SkuId = skuId,
                                    LanguageCode = "bg",
                                    Name = marketTask.Name,
                                    Description = $"{marketTask.Name} ({marketTask.Unit})",
                                    UnitType = marketTask.Unit,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                });

                                await ServiceSkus.AddAsync(newSku);
                                count++;
                            }
                        }
                    }
                }
            }
        }

        // 2. Seed from Electrical_SKUs_Seed.json (Specific Electrical Data)
        var elecPath = Path.Combine(AppContext.BaseDirectory, "Electrical_SKUs_Seed.json");
        if (System.IO.File.Exists(elecPath))
        {
            var json = await System.IO.File.ReadAllTextAsync(elecPath);
            var elecData = System.Text.Json.JsonSerializer.Deserialize<ElectricalSeedDto>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (elecData != null)
            {
                var category = await ServiceCategories.FirstOrDefaultAsync(c => c.Name == "Електрическа Инсталация" || c.Name == "Electrical");
                if (category != null)
                {
                    foreach (var skuDto in elecData.Skus)
                    {
                        var existing = await ServiceSkus.Include(s => s.Translations).FirstOrDefaultAsync(s => s.SkuCode == skuDto.SkuCode);
                        if (existing == null)
                        {
                            var skuId = Guid.NewGuid();
                            var newSku = new ServiceSku
                            {
                                Id = skuId,
                                ServiceCategoryId = category.Id,
                                SkuCode = skuDto.SkuCode,
                                Name = skuDto.Name,
                                Description = skuDto.Description,
                                BasePrice = skuDto.BasePrice,
                                UnitType = skuDto.UnitType,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };

                            newSku.Translations.Add(new ServiceSkuTranslation
                            {
                                Id = Guid.NewGuid(),
                                SkuId = skuId,
                                LanguageCode = "bg",
                                Name = skuDto.Name,
                                Description = skuDto.Description,
                                UnitType = skuDto.UnitType,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            });

                            await ServiceSkus.AddAsync(newSku);
                        }
                    }
                }
            }
        }

        await SaveChangesAsync();
    }

    private string MapMarketCategoryToDbName(string marketName)
    {
        if (marketName.Contains("Demolition")) return "Къртене и извозване (Demolition)";
        if (marketName.Contains("Drywall")) return "Сухо строителство (Drywall)";
        if (marketName.Contains("Painting")) return "Бояджийски и шпакловъчни услуги (Painting)";
        if (marketName.Contains("Tiling")) return "Подови и стенни настилки (Tiling)";
        if (marketName.Contains("Plumbing")) return "ВиК Услуги (Plumbing)";
        if (marketName.Contains("Electrical")) return "Електрическа Инсталация";
        return marketName;
    }

    private string GetCategoryPrefix(string dbName)
    {
        if (dbName.Contains("Demolition")) return "DEMO";
        if (dbName.Contains("Drywall")) return "DRYW";
        if (dbName.Contains("Painting")) return "PANT";
        if (dbName.Contains("Tiling")) return "TILE";
        if (dbName.Contains("Plumbing")) return "PLMB";
        if (dbName.Contains("Electrical")) return "ELEC";
        return "GEN";
    }

    private string MapMarketUnitToUnitType(string marketUnit)
    {
        if (marketUnit.Contains("кв.м")) return "sqm";
        if (marketUnit.Contains("лин.м")) return "m";
        if (marketUnit.Contains("бр")) return "pcs";
        if (marketUnit.Contains("курс")) return "trip";
        if (marketUnit.Contains("куб.м")) return "m3";
        return "pcs";
    }

    private class CategorySeedDto
    {
        public string Name { get; set; } = string.Empty;
        public object? TemplateStructure { get; set; }
    }

    private class MarketCategorySeedDto
    {
        public string Category { get; set; } = string.Empty;
        public List<MarketTaskSeedDto> Tasks { get; set; } = new();
    }

    private class MarketTaskSeedDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    private class ElectricalSeedDto
    {
        public List<ElectricalSkuSeedDto> Skus { get; set; } = new();
    }

    private class ElectricalSkuSeedDto
    {
        public string SkuCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public string UnitType { get; set; } = string.Empty;
    }
}
