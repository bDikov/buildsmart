using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Entities.JoinEntities;
using BuildSmart.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Sentry;

namespace BuildSmart.Infrastructure.Persistence;

public partial class AppDbContext : DbContext
{
	// Define DbSets only for your Aggregate Roots
	public DbSet<User> Users { get; set; } = null!;
    public DbSet<HomeownerProfile> HomeownerProfiles { get; set; } = null!;
	public DbSet<TradesmanSkill> TradesmanSkills { get; set; } = null!;
	public DbSet<ServiceCategory> ServiceCategories { get; set; } = null!;
	public DbSet<ServiceSku> ServiceSkus { get; set; } = null!;
	public DbSet<TradesmanProfile> TradesmanProfiles { get; set; } = null!;
	public DbSet<Project> Projects { get; set; } = null!;
	public DbSet<JobPost> JobPosts { get; set; } = null!;
	public DbSet<JobPostQuestion> JobPostQuestions { get; set; } = null!;
    public DbSet<JobPostFeedback> JobPostFeedbacks { get; set; } = null!;
	public DbSet<Bid> Bids { get; set; } = null!;
	public DbSet<Question> Questions { get; set; } = null!;
	public DbSet<Formula> Formulas { get; set; } = null!;
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
	public DbSet<TradesmanMedia> TradesmanMedia { get; set; } = null!;
	public DbSet<ProjectMilestoneMedia> ProjectMilestoneMedia { get; set; } = null!;
	public DbSet<ProjectMessage> ProjectMessages { get; set; } = null!;
	public DbSet<LocalizationResource> LocalizationResources { get; set; } = null!;
	public DbSet<UserCampaignMetadata> UserCampaignMetadata { get; set; } = null!;
	public DbSet<BlogPost> BlogPosts { get; set; } = null!;
	public DbSet<LandingPageContent> LandingPages { get; set; } = null!;
	public DbSet<CalculatorLead> CalculatorLeads { get; set; } = null!;
	public DbSet<TaskComment> TaskComments { get; set; } = null!;
	public DbSet<CategoryTradesmanAssignment> CategoryTradesmanAssignments { get; set; } = null!;
	public DbSet<TaskPaymentRecord> TaskPaymentRecords { get; set; } = null!;

	public AppDbContext(DbContextOptions<AppDbContext> options)
		: base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
		
		modelBuilder.Entity<BlogPost>(entity =>
		{
			entity.HasKey(e => e.Id);
			entity.HasIndex(e => e.Slug).IsUnique();
			entity.Property(e => e.Slug).HasMaxLength(200).IsRequired();
			entity.Property(e => e.TitleBg).HasMaxLength(300).IsRequired();
			entity.Property(e => e.TitleEn).HasMaxLength(300).IsRequired();
		});

		modelBuilder.Entity<UserCampaignMetadata>(entity =>
		{
			entity.HasOne(d => d.User)
				.WithOne(p => p.CampaignMetadata)
				.HasForeignKey<UserCampaignMetadata>(d => d.UserId)
				.OnDelete(DeleteBehavior.Cascade);
		});
		
		modelBuilder.Entity<TradesmanMedia>()
			.HasOne(m => m.TradesmanProfile)
			.WithMany(p => p.Media)
			.HasForeignKey(m => m.TradesmanId)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<ProjectMilestoneMedia>()
			.HasOne(m => m.TradesmanProfile)
			.WithMany(p => p.MilestoneMedia)
			.HasForeignKey(m => m.TradesmanProfileId)
			.OnDelete(DeleteBehavior.Cascade);

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
        var paintingCategory = await ServiceCategories.FirstOrDefaultAsync(c => c.Name == "Бояджийски и шпакловъчни услуги");
        if (paintingCategory == null)
        {
            paintingCategory = new ServiceCategory
            {
                Id = Guid.NewGuid(),
                Name = "Бояджийски и шпакловъчни услуги",
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

    private async Task<string> ReadEmbeddedResourceAsync(string fileName)
    {
        var assembly = typeof(AppDbContext).Assembly;
        var resourceName = $"BuildSmart.Infrastructure.{fileName}";
        
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            var availableResources = string.Join(", ", assembly.GetManifestResourceNames());
            throw new FileNotFoundException($"Embedded resource '{resourceName}' not found. Available: {availableResources}");
        }

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    public async Task CleanupAndMergeCategoriesAsync()
    {
        var suffixMap = new Dictionary<string, string>
        {
            { "Electrical", "Електрическа Инсталация" },
            { "Painting", "Бояджийски и шпакловъчни услуги" },
            { "Plumbing", "ВиК Услуги" },
            { "Demolition", "Къртене и извозване" },
            { "Drywall", "Сухо строителство" },
            { "Tiling", "Подови и стенни настилки" },
            { "Microcement", "Микроцимент" },
            { "ВиК Услуги (Plumbing)", "ВиК Услуги" },
            { "Бояджийски и шпакловъчни услуги (Painting)", "Бояджийски и шпакловъчни услуги" },
            { "Къртене и извозване (Demolition)", "Къртене и извозване" },
            { "Сухо строителство (Drywall)", "Сухо строителство" },
            { "Подови и стенни настилки (Tiling)", "Подови и стенни настилки" },
            { "Микроцимент (Microcement)", "Микроцимент" },
            { "Електрическа Инсталация ", "Електрическа Инсталация" }
        };

        foreach (var entry in suffixMap)
        {
            var suffixName = entry.Key;
            var cleanName = entry.Value;

            var suffixCategory = await ServiceCategories.FirstOrDefaultAsync(c => c.Name == suffixName);
            if (suffixCategory == null) continue;

            var cleanCategory = await ServiceCategories.FirstOrDefaultAsync(c => c.Name == cleanName);
            if (cleanCategory == null)
            {
                suffixCategory.Name = cleanName;
                suffixCategory.UpdatedAt = DateTime.UtcNow;
                await SaveChangesAsync();
                Console.WriteLine($"Renamed category '{suffixName}' to '{cleanName}'");
            }
            else
            {
                Console.WriteLine($"Merging category '{suffixName}' into '{cleanName}'...");

                // Questions
                var questions = await Questions.Where(q => q.ServiceCategoryId == suffixCategory.Id).ToListAsync();
                foreach (var question in questions)
                {
                    question.ServiceCategoryId = cleanCategory.Id;
                    question.UpdatedAt = DateTime.UtcNow;
                }

                // ServiceSkus
                var skus = await ServiceSkus.Where(s => s.ServiceCategoryId == suffixCategory.Id).ToListAsync();
                foreach (var sku in skus)
                {
                    var existingSku = await ServiceSkus.FirstOrDefaultAsync(s => s.ServiceCategoryId == cleanCategory.Id && s.SkuCode == sku.SkuCode);
                    if (existingSku == null)
                    {
                        sku.ServiceCategoryId = cleanCategory.Id;
                        sku.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        // Update references in related tables to point to the existing clean SKU instead of the duplicate about to be deleted
                        var affectedAiItems = await AiCalculationSkuItems.Where(item => item.ServiceSkuId == sku.Id).ToListAsync();
                        foreach (var item in affectedAiItems)
                        {
                            item.ServiceSkuId = existingSku.Id;
                        }

                        var affectedTaskItems = await TaskSkuItems.Where(item => item.ServiceSkuId == sku.Id).ToListAsync();
                        foreach (var item in affectedTaskItems)
                        {
                            item.ServiceSkuId = existingSku.Id;
                        }

                        ServiceSkus.Remove(sku);
                    }
                }

                // TradesmanSkills
                var skills = await TradesmanSkills.Where(ts => ts.ServiceCategoryId == suffixCategory.Id).ToListAsync();
                foreach (var skill in skills)
                {
                    var exists = await TradesmanSkills.AnyAsync(ts => ts.TradesmanProfileId == skill.TradesmanProfileId && ts.ServiceCategoryId == cleanCategory.Id);
                    if (!exists)
                    {
                        skill.ServiceCategoryId = cleanCategory.Id;
                    }
                    else
                    {
                        TradesmanSkills.Remove(skill);
                    }
                }

                // TradesmanMedia
                var mediaList = await TradesmanMedia.Where(tm => tm.ServiceCategoryId == suffixCategory.Id).ToListAsync();
                foreach (var media in mediaList)
                {
                    media.ServiceCategoryId = cleanCategory.Id;
                    media.UpdatedAt = DateTime.UtcNow;
                }

                // JobPosts
                var jobs = await JobPosts.Where(jp => jp.ServiceCategoryId == suffixCategory.Id).ToListAsync();
                foreach (var job in jobs)
                {
                    job.ServiceCategoryId = cleanCategory.Id;
                    job.UpdatedAt = DateTime.UtcNow;
                }

                ServiceCategories.Remove(suffixCategory);
                await SaveChangesAsync();
                Console.WriteLine($"Merged and removed category '{suffixName}'");
            }
        }
    }

    public async Task SeedCategoriesAndQuestionsAsync()
    {
        try
        {
            var json = await ReadEmbeddedResourceAsync("Categories_Seed_Templates.json");
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var seedData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, CategorySeedDto>>(json, options);
            
            if (seedData == null) return;

            foreach (var kvp in seedData)
            {
                var categoryName = kvp.Value.Name;
                var categoryType = kvp.Key switch {
                    "user_category" => CategoryType.UserType,
                    "global_category" => CategoryType.Global,
                    "project_details_category" => CategoryType.ProjectDetails,
                    _ => CategoryType.CategorySpecific
                };
                var isGlobal = categoryType == CategoryType.Global;

                var category = await ServiceCategories.FirstOrDefaultAsync(c => c.Name == categoryName);
                if (category == null)
                {
                    category = new ServiceCategory
                    {
                        Id = Guid.NewGuid(),
                        Name = categoryName,
                        Status = CategoryStatus.Active,
                        IsGlobal = isGlobal,
                        Type = categoryType,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await ServiceCategories.AddAsync(category);
                }
                else
                {
                    category.IsGlobal = isGlobal;
                    category.Type = categoryType;
                }

                // Add or update translations from the JSON
                if (kvp.Value.Translations != null)
                {
                    if (kvp.Value.Translations.TryGetValue("en", out var enName))
                    {
                        category.EnglishName = enName;
                    }
                }
                
                category.TemplateStructure = System.Text.Json.JsonSerializer.Serialize(kvp.Value.TemplateStructure);
            }

            await SaveChangesAsync();
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Console.WriteLine($"Error seeding categories: {ex.Message}");
            throw;
        }
    }

    public async Task SeedSkusAsync()
    {
        Console.WriteLine("--- SKU SEEDING START (Embedded Resources) ---");
        
        var allDbCategories = await ServiceCategories.ToListAsync();
        if (allDbCategories.Count == 0)
        {
             throw new Exception("SKU Seeding aborted: ServiceCategories table is empty!");
        }

        // 1. Seed from MarketData_Sofia_Seed.json
        try
        {
            var json = await ReadEmbeddedResourceAsync("MarketData_Sofia_Seed.json");
            var marketData = System.Text.Json.JsonSerializer.Deserialize<List<MarketCategorySeedDto>>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (marketData != null)
            {
                foreach (var marketCat in marketData)
                {
                    var dbCategoryName = MapMarketCategoryToDbName(marketCat.Category);
                    var category = allDbCategories.FirstOrDefault(c => c.Name == dbCategoryName);
                    
                    if (category != null)
                    {
                        var prefix = GetCategoryPrefix(dbCategoryName);
                        int count = 1;
                        foreach (var marketTask in marketCat.Tasks)
                        {
                            var skuCode = $"{prefix}-{count:D3}";
                            var existingSkus = await ServiceSkus.Where(s => s.SkuCode == skuCode).ToListAsync();
                            
                            if (!existingSkus.Any())
                            {
                                var skuId = Guid.NewGuid();
                                var newSku = new ServiceSku
                                {
                                    Id = skuId,
                                    ServiceCategoryId = category.Id,
                                    SkuCode = skuCode,
                                    Name = marketTask.Name,
                                    Description = $"{marketTask.Name} ({marketTask.Unit})",
                                    BasePrice = Math.Round(marketTask.MaxPrice / 1.95583m, 2),
                                    UnitType = MapMarketUnitToUnitType(marketTask.Unit),
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                };

                                await ServiceSkus.AddAsync(newSku);
                            }
                            else
                            {
                                foreach (var existingSku in existingSkus)
                                {
                                    existingSku.Name = marketTask.Name;
                                    existingSku.Description = $"{marketTask.Name} ({marketTask.Unit})";
                                    existingSku.BasePrice = Math.Round(marketTask.MaxPrice / 1.95583m, 2);
                                    existingSku.UnitType = MapMarketUnitToUnitType(marketTask.Unit);
                                    existingSku.UpdatedAt = DateTime.UtcNow;
                                }
                            }
                            count++;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error seeding market SKUs: {ex.Message}");
            SentrySdk.CaptureException(ex);
        }

        // 2. Seed from Category SKU JSON files
        var seedFiles = new[]
        {
            new { FileName = "Global_SKUs_Seed.json", CategoryName = "Global Questions" },
            new { FileName = "Electrical_SKUs_Seed.json", CategoryName = "Електрическа Инсталация" },
            new { FileName = "Painting_SKUs_Seed.json", CategoryName = "Бояджийски и шпакловъчни услуги" },
            new { FileName = "Drywall_SKUs_Seed.json", CategoryName = "Сухо строителство" },
            new { FileName = "Tiling_SKUs_Seed.json", CategoryName = "Подови и стенни настилки" },
            new { FileName = "Microcement_SKUs_Seed.json", CategoryName = "Микроцимент" },
            new { FileName = "Plumbing_SKUs_Seed.json", CategoryName = "ВиК Услуги" },
            new { FileName = "Demolition_SKUs_Seed.json", CategoryName = "Къртене и извозване" }
        };

        foreach (var seed in seedFiles)
        {
            try
            {
                var json = await ReadEmbeddedResourceAsync(seed.FileName);
                var data = System.Text.Json.JsonSerializer.Deserialize<ElectricalSeedDto>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (data != null)
                {
                    var category = allDbCategories.FirstOrDefault(c => c.Name == seed.CategoryName);
                    if (category != null)
                    {
                        foreach (var skuDto in data.Skus)
                        {
                            var eurPrice = Math.Round(skuDto.BasePrice / 1.95583m, 2);
                            var existing = await ServiceSkus.FirstOrDefaultAsync(s => s.SkuCode == skuDto.SkuCode);
                            
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
                                    BasePrice = eurPrice,
                                    UnitType = skuDto.UnitType,
                                    CalculationFormula = skuDto.CalculationFormula,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                };

                                await ServiceSkus.AddAsync(newSku);
                            }
                            else
                            {
                                // Sync properties and formula
                                existing.ServiceCategoryId = category.Id;
                                existing.Name = skuDto.Name;
                                existing.Description = skuDto.Description;
                                existing.BasePrice = eurPrice;
                                existing.UnitType = skuDto.UnitType;
                                existing.CalculationFormula = skuDto.CalculationFormula;
                                existing.UpdatedAt = DateTime.UtcNow;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error seeding SKUs from file '{seed.FileName}': {ex.Message}");
                SentrySdk.CaptureException(ex);
            }
        }
        
        await CleanupObsoleteLegacySkusAsync();

        await SaveChangesAsync();
        
        var finalCount = await ServiceSkus.CountAsync();
        Console.WriteLine($"--- SKU SEEDING FINISHED. Total SKUs: {finalCount} ---");
        SentrySdk.CaptureMessage($"Seeding Complete. Total SKUs: {finalCount}", SentryLevel.Info);
    }

    private async Task CleanupObsoleteLegacySkusAsync()
    {
        Console.WriteLine("--- CLEANING UP OBSOLETE LEGACY SKUS START ---");
        
        var legacyToNewMap = new Dictionary<string, string>
        {
            { "PANT-001", "PANT-PRIMER" },
            { "PANT-002", "PANT-SPACKLE-STD" },
            { "PANT-003", "PANT-PAINT-WHITE" },
            { "PANT-005", "PANT-TRIM" },
            { "PANT-006", "PANT-SPACKLE-Q5" },
            { "TILE-001", "TILE-STD" },
            { "TILE-003", "TILE-LAMINATE" },
            { "TILE-004", "TILE-PREP-LEVEL" },
            { "DEMO-001", "DEMO-FLOOR-TILE" },
            { "DEMO-002", "DEMO-WALL-CONC" },
            { "DRYW-INSUL-WALL", "DRYW-INSUL-PARTITION" },
            { "DRYW-INSULATION", "DRYW-INSUL-PARTITION" }
        };

        foreach (var entry in legacyToNewMap)
        {
            var oldCode = entry.Key;
            var newCode = entry.Value;

            var oldSku = await ServiceSkus.FirstOrDefaultAsync(s => s.SkuCode == oldCode);
            if (oldSku == null) continue;

            var newSku = await ServiceSkus.FirstOrDefaultAsync(s => s.SkuCode == newCode);
            if (newSku != null)
            {
                Console.WriteLine($"Merging obsolete legacy SKU '{oldCode}' into new SKU '{newCode}'...");

                // Re-link related items to the new SKU
                var affectedAiItems = await AiCalculationSkuItems.Where(item => item.ServiceSkuId == oldSku.Id).ToListAsync();
                foreach (var item in affectedAiItems)
                {
                    item.ServiceSkuId = newSku.Id;
                }

                var affectedTaskItems = await TaskSkuItems.Where(item => item.ServiceSkuId == oldSku.Id).ToListAsync();
                foreach (var item in affectedTaskItems)
                {
                    item.ServiceSkuId = newSku.Id;
                }

                ServiceSkus.Remove(oldSku);
            }
            else
            {
                // If the new SKU is not seeded yet, rename the old one to the new code
                oldSku.SkuCode = newCode;
                oldSku.UpdatedAt = DateTime.UtcNow;
                Console.WriteLine($"Renamed legacy SKU '{oldCode}' to '{newCode}'");
            }
        }

        // Also delete remaining duplicate obsolete paint codes (e.g. PANT-004 has no direct single equivalent)
        var remainingObsoleteCodes = new[] { "PANT-004" };
        foreach (var code in remainingObsoleteCodes)
        {
            var oldSku = await ServiceSkus.FirstOrDefaultAsync(s => s.SkuCode == code);
            if (oldSku != null)
            {
                ServiceSkus.Remove(oldSku);
                Console.WriteLine($"Deleted obsolete legacy SKU '{code}'");
            }
        }
    }

    public static readonly Dictionary<string, (string Formula, string UnitType, decimal? BasePrice)> LegacySkuFormulas = new()
    {
        // Painting
        { "PANT-001", ("global_total_sqm * if(Contains(global_ceiling_height, 'Висока'), 2.8, 2.5)", "sqm", 1.53m) },
        { "PANT-002", ("if(Contains(paint_tasks, 'Цялостна шпакловка') || Contains(paint_tasks, 'Сваляне на тапети'), global_total_sqm * if(Contains(global_ceiling_height, 'Висока'), 2.8, 2.5), 0)", "sqm", 4.09m) },
        { "PANT-003", ("global_total_sqm * if(Contains(global_ceiling_height, 'Висока'), 2.8, 2.5)", "sqm", 3.32m) },
        { "PANT-004", ("if(Contains(paint_tasks, 'Цялостна шпакловка') || Contains(paint_tasks, 'Сваляне на тапети'), global_total_sqm * if(Contains(global_ceiling_height, 'Висока'), 2.8, 2.5), 0)", "sqm", 1.02m) },
        { "PANT-005", ("if(Contains(paint_trim_doors_count, '4+'), 4, if(Contains(paint_trim_doors_count, '3'), 3, if(Contains(paint_trim_doors_count, '2'), 2, if(Contains(paint_trim_doors_count, '1'), 1, 0))))", "pcs", 23.01m) },
        { "PANT-006", ("if(Contains(paint_finish_level, 'Q5') || Contains(paint_finish_level, 'Перфектно'), global_total_sqm * if(Contains(global_ceiling_height, 'Висока'), 2.8, 2.5), 0)", "sqm", 7.67m) },
        
        // Tiling
        { "TILE-001", ("if(tile_std_sqm > 0, tile_std_sqm, global_total_sqm * 0.3)", "sqm", null) },
        { "TILE-003", ("if(tile_laminate_sqm > 0, tile_laminate_sqm, global_total_sqm * 0.7)", "sqm", null) },
        { "TILE-004", ("if(tile_prep_level_sqm > 0, tile_prep_level_sqm, global_total_sqm * 0.5)", "sqm", null) },
        
        // Demolition
        { "DEMO-001", ("if(demo_floor_sqm > 0, demo_floor_sqm, global_total_sqm * 0.3)", "sqm", null) },
        { "DEMO-002", ("if(demo_conc_sqm > 0, demo_conc_sqm, global_total_sqm * 0.2)", "sqm", null) }
    };

    public async Task SeedQuestionsAndFormulasAsync()
    {
        Console.WriteLine("--- QUESTIONS AND FORMULAS SEEDING START ---");

        var categories = await ServiceCategories.ToListAsync();
        var allSkus = await ServiceSkus.ToListAsync();

        // Load existing database questions
        var existingQuestions = await Questions.ToDictionaryAsync(q => q.QuestionCode);
        var seededQuestions = new Dictionary<string, Question>(existingQuestions);

        foreach (var category in categories)
        {
            if (string.IsNullOrWhiteSpace(category.TemplateStructure) || category.TemplateStructure == "{}")
                continue;

            try
            {
                using var doc = JsonDocument.Parse(category.TemplateStructure);
                if (doc.RootElement.TryGetProperty("questions", out var questionsArr))
                {
                    int displayOrder = 1;
                    foreach (var qJson in questionsArr.EnumerateArray())
                    {
                        var code = qJson.GetProperty("id").GetString();
                        if (string.IsNullOrEmpty(code)) continue;

                        var textProp = qJson.GetProperty("text");
                        var text = textProp.ValueKind == JsonValueKind.String 
                            ? textProp.GetString() ?? string.Empty 
                            : (textProp.TryGetProperty("bg", out var bgProp) ? bgProp.GetString() : textProp.GetRawText()) ?? string.Empty;

                        var type = qJson.GetProperty("type").GetString() ?? "text";
                        var required = qJson.TryGetProperty("required", out var reqProp) && reqProp.GetBoolean();
                        var hintText = qJson.TryGetProperty("hintText", out var hintProp) ? hintProp.GetString() : null;
                        
                        string? optionsJson = null;
                        if (qJson.TryGetProperty("options", out var optProp) && optProp.ValueKind == JsonValueKind.Array)
                        {
                            optionsJson = optProp.GetRawText();
                        }

                        var dependsOn = qJson.TryGetProperty("dependsOn", out var depProp) ? depProp.GetString() : null;
                        var dependsOnValue = qJson.TryGetProperty("dependsOnValue", out var depValProp) ? depValProp.GetString() : null;

                        if (!seededQuestions.TryGetValue(code, out var question))
                        {
                            question = new Question
                            {
                                Id = Guid.NewGuid(),
                                QuestionCode = code,
                                Text = text,
                                Type = type,
                                IsRequired = required,
                                HintText = hintText,
                                OptionsJson = optionsJson,
                                ServiceCategoryId = category.Id,
                                DisplayOrder = displayOrder++,
                                VisibilityCondition = dependsOnValue,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            await Questions.AddAsync(question);
                            seededQuestions[code] = question;
                        }
                        else
                        {
                            // Preserve existing question state to prevent seeding from overwriting custom admin-panel changes on startup.
                            if (question.ServiceCategoryId == null)
                            {
                                question.ServiceCategoryId = category.Id;
                                question.UpdatedAt = DateTime.UtcNow;
                                if (existingQuestions.ContainsKey(code))
                                {
                                    Questions.Update(question);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing category {category.Name} template structure during seeding: {ex.Message}");
            }
        }

        await SaveChangesAsync();

        // 2. Set ParentQuestionId (dependsOn relations)
        var allQuestions = await Questions.ToListAsync();
        foreach (var category in categories)
        {
            if (string.IsNullOrWhiteSpace(category.TemplateStructure) || category.TemplateStructure == "{}")
                continue;

            try
            {
                using var doc = JsonDocument.Parse(category.TemplateStructure);
                if (doc.RootElement.TryGetProperty("questions", out var questionsArr))
                {
                    foreach (var qJson in questionsArr.EnumerateArray())
                    {
                        var code = qJson.GetProperty("id").GetString();
                        var dependsOn = qJson.TryGetProperty("dependsOn", out var depProp) ? depProp.GetString() : null;

                        if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(dependsOn))
                        {
                            var childQ = allQuestions.FirstOrDefault(q => q.QuestionCode == code);
                            var parentQ = allQuestions.FirstOrDefault(q => q.QuestionCode == dependsOn);
                            if (childQ != null && parentQ != null && childQ.ParentQuestionId != parentQ.Id)
                            {
                                childQ.ParentQuestionId = parentQ.Id;
                                Questions.Update(childQ);
                            }
                        }
                    }
                }
            }
            catch {}
        }

        await SaveChangesAsync();

        // 3. Link Questions to SKUs based on SKU calculationFormula matching question codes
        allQuestions = await Questions.ToListAsync();
        foreach (var sku in allSkus)
        {
            if (string.IsNullOrWhiteSpace(sku.CalculationFormula)) continue;

            // Simple parser: check if formula mentions any question codes
            foreach (var q in allQuestions)
            {
                if (sku.CalculationFormula.Contains(q.QuestionCode))
                {
                    // Check if link exists
                    var linkExists = await Entry(q).Collection(x => x.Skus).Query().AnyAsync(s => s.Id == sku.Id);
                    if (!linkExists)
                    {
                        q.Skus.Add(sku);
                        if (!q.SkuIds.Contains(sku.Id))
                        {
                            q.SkuIds.Add(sku.Id);
                        }
                        Questions.Update(q);
                    }
                }
            }
        }

        await SaveChangesAsync();

        // 4. Seed Standard Formulas
        var standardFormulas = new List<(string Name, string Description, string Expression)>
        {
            ("paint_area_calc", "Изчислява общата площ на стени и тавани за боядисване", "global_total_sqm * if(Contains(global_ceiling_height, 'Висока'), 2.8, 2.5)"),
            ("tiling_area_calc", "Стандартна площ за лепене на плочки", "if(tile_std_sqm > 0, tile_std_sqm, global_total_sqm * 0.3)"),
            ("drywall_area_calc", "Приблизителна площ за монтаж на гипсокартон", "global_total_sqm * 1.2")
        };

        foreach (var sf in standardFormulas)
        {
            var exists = await Formulas.AnyAsync(f => f.Name == sf.Name);
            if (!exists)
            {
                var formula = new Formula
                {
                    Id = Guid.NewGuid(),
                    Name = sf.Name,
                    Description = sf.Description,
                    Expression = sf.Expression,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await Formulas.AddAsync(formula);
            }
        }

        await SaveChangesAsync();

        Console.WriteLine("--- QUESTIONS AND FORMULAS SEEDING COMPLETE ---");
    }

    private string MapMarketCategoryToDbName(string marketName)
    {
        if (marketName.Contains("Demolition")) return "Къртене и извозване";
        if (marketName.Contains("Drywall")) return "Сухо строителство";
        if (marketName.Contains("Painting")) return "Бояджийски и шпакловъчни услуги";
        if (marketName.Contains("Tiling")) return "Подови и стенни настилки";
        if (marketName.Contains("Plumbing")) return "ВиК Услуги";
        if (marketName.Contains("Electrical")) return "Електрическа Инсталация";
        return marketName;
    }

    private string GetCategoryPrefix(string dbName)
    {
        if (dbName.Contains("Demolition") || dbName.Contains("Къртене")) return "DEMO";
        if (dbName.Contains("Drywall") || dbName.Contains("Сухо")) return "DRYW";
        if (dbName.Contains("Painting") || dbName.Contains("Бояджийски")) return "PANT";
        if (dbName.Contains("Tiling") || dbName.Contains("настилки")) return "TILE";
        if (dbName.Contains("Plumbing") || dbName.Contains("ВиК")) return "PLMB";
        if (dbName.Contains("Electrical") || dbName.Contains("Електрическа")) return "ELEC";
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
        public Dictionary<string, string>? Translations { get; set; }
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
        public string CalculationFormula { get; set; } = string.Empty;
    }
}
