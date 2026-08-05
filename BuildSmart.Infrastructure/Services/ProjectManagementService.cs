using BuildSmart.Core.Application.DTOs;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using BuildSmart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using TaskStatus = BuildSmart.Core.Domain.Enums.TaskStatus;

namespace BuildSmart.Infrastructure.Services;

public class ProjectManagementService : IProjectManagementService
{
    private readonly AppDbContext _context;

    public ProjectManagementService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserLookupDto>> GetHomeownersLookupAsync()
    {
        var users = await _context.Users
            .AsNoTracking()
            .Where(u => u.Role != UserRoleTypes.Tradesman && (u.Email == null || !u.Email.EndsWith("@buildsmart.guest")))
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync();

        if (!users.Any())
        {
            users = await _context.Users
                .AsNoTracking()
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();
        }

        return users.Select(u => {
            var name = $"{u.FirstName} {u.LastName}".Trim();
            var email = string.IsNullOrWhiteSpace(u.Email) ? "No Email" : u.Email.Trim();
            var displayName = !string.IsNullOrWhiteSpace(name)
                ? name
                : (!string.IsNullOrWhiteSpace(u.Email) ? u.Email : $"User ({u.Id.ToString().Substring(0, 8)})");

            return new UserLookupDto
            {
                UserId = u.Id,
                FullName = displayName,
                Email = email,
                PhoneNumber = u.PhoneNumber
            };
        }).ToList();
    }

    public async Task<List<UserLookupDto>> GetTradesmenLookupAsync()
    {
        var users = await _context.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRoleTypes.Tradesman)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync();

        if (!users.Any())
        {
            users = await _context.Users
                .AsNoTracking()
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();
        }

        return users.Select(u => {
            var name = $"{u.FirstName} {u.LastName}".Trim();
            var email = string.IsNullOrWhiteSpace(u.Email) ? "No Email" : u.Email.Trim();
            var displayName = !string.IsNullOrWhiteSpace(name)
                ? name
                : (!string.IsNullOrWhiteSpace(u.Email) ? u.Email : $"Tradesman ({u.Id.ToString().Substring(0, 8)})");

            return new UserLookupDto
            {
                UserId = u.Id,
                FullName = displayName,
                Email = email,
                PhoneNumber = u.PhoneNumber
            };
        }).ToList();
    }

    public async Task<Guid> CreateProjectForUserAsync(Guid homeownerUserId, string title, string description, List<Guid> serviceCategoryIds, string? location, Dictionary<Guid, Guid>? categoryTradesmanMap = null, decimal adminMarkupPercentage = 20.0m)
    {
        var homeowner = await _context.Users
            .Include(u => u.HomeownerProfile)
            .FirstOrDefaultAsync(u => u.Id == homeownerUserId);

        if (homeowner == null)
            throw new ArgumentException("Specified Homeowner user does not exist.");

        if (homeowner.HomeownerProfile == null)
        {
            homeowner.HomeownerProfile = new HomeownerProfile
            {
                Id = Guid.NewGuid(),
                UserId = homeowner.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        var project = new Project
        {
            Id = Guid.NewGuid(),
            HomeownerId = homeowner.Id,
            Homeowner = homeowner,
            Title = string.IsNullOrWhiteSpace(title) ? "New Renovation Project" : title,
            Description = description ?? string.Empty,
            GeneralSummary = description,
            AdminMarkupPercentage = adminMarkupPercentage < 0 ? 0 : adminMarkupPercentage,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Projects.Add(project);

        var categories = await _context.ServiceCategories
            .Where(c => serviceCategoryIds.Contains(c.Id))
            .ToListAsync();

        var skus = await _context.ServiceSkus
            .Where(s => serviceCategoryIds.Contains(s.ServiceCategoryId))
            .ToListAsync();

        int categoryIndex = 0;
        foreach (var category in categories)
        {
            categoryIndex++;
            var jobPost = new JobPost
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                HomeownerProfileId = homeowner.HomeownerProfile.Id,
                ServiceCategoryId = category.Id,
                Title = $"{category.Name} - Work Package",
                Description = $"Auto-configured work package for {category.Name}",
                Location = location ?? "Sofia",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.JobPosts.Add(jobPost);

            if (categoryTradesmanMap != null && categoryTradesmanMap.TryGetValue(category.Id, out var tradesmanId) && tradesmanId != Guid.Empty)
            {
                jobPost.AssignedTradesmanId = tradesmanId;
                _context.CategoryTradesmanAssignments.Add(new CategoryTradesmanAssignment
                {
                    Id = Guid.NewGuid(),
                    ProjectId = project.Id,
                    JobPostId = jobPost.Id,
                    ServiceCategoryId = category.Id,
                    TradesmanId = tradesmanId,
                    AssignedByAdminId = homeownerUserId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            jobPost.Publish();

            int taskSequence = 0;
            var categorySkus = skus.Where(s => s.ServiceCategoryId == category.Id).Take(5).ToList();
            decimal markupFactor = 1.0m + (adminMarkupPercentage / 100.0m);

            foreach (var sku in categorySkus)
            {
                taskSequence++;
                int quantity = 10;
                decimal tradesmanTotalEur = Math.Round((sku.BasePrice / 1.95583m) * quantity, 2);
                decimal homeownerTotalEur = Math.Round(tradesmanTotalEur * markupFactor, 2);

                var task = new JobTask
                {
                    Id = Guid.NewGuid(),
                    JobPostId = jobPost.Id,
                    Title = sku.Name,
                    Description = sku.Description ?? sku.Name,
                    SequenceOrder = taskSequence,
                    TradesmanPrice = tradesmanTotalEur,
                    EstimatedPrice = homeownerTotalEur,
                    Status = TaskStatus.ToDo,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var skuItem = new TaskSkuItem
                {
                    Id = Guid.NewGuid(),
                    JobTaskId = task.Id,
                    ServiceSkuId = sku.Id,
                    Quantity = quantity,
                    EstimatedPrice = tradesmanTotalEur,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                task.SkuItems.Add(skuItem);

                var criteria = new TaskAcceptanceCriteria
                {
                    Id = Guid.NewGuid(),
                    JobTaskId = task.Id,
                    Description = $"Quality verification for {sku.Name} according to Bulgarian Construction Standards (BDS).",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                task.AcceptanceCriteria.Add(criteria);

                jobPost.JobTasks.Add(task);

                _context.JobTasks.Add(task);
                _context.TaskSkuItems.Add(skuItem);
                _context.TaskAcceptanceCriteria.Add(criteria);
            }
        }

        await _context.SaveChangesAsync();
        return project.Id;
    }

    public async Task<Guid> CreateProjectFromOfferTemplateAsync(
        Guid homeownerUserId,
        string title,
        string description,
        string? location,
        Dictionary<Guid, Guid>? categoryTradesmanMap,
        decimal adminMarkupPercentage,
        List<CustomOfferPhaseDto> phases)
    {
        var homeowner = await _context.Users
            .Include(u => u.HomeownerProfile)
            .FirstOrDefaultAsync(u => u.Id == homeownerUserId);

        if (homeowner == null)
            throw new ArgumentException("Specified Homeowner user does not exist.");

        if (homeowner.HomeownerProfile == null)
        {
            homeowner.HomeownerProfile = new HomeownerProfile
            {
                Id = Guid.NewGuid(),
                UserId = homeowner.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        var project = new Project
        {
            Id = Guid.NewGuid(),
            HomeownerId = homeowner.Id,
            Homeowner = homeowner,
            Title = string.IsNullOrWhiteSpace(title) ? "Tradesman Offer Project" : title,
            Description = description ?? string.Empty,
            GeneralSummary = description,
            AdminMarkupPercentage = adminMarkupPercentage < 0 ? 0 : adminMarkupPercentage,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Projects.Add(project);

        var defaultCategory = await _context.ServiceCategories.FirstOrDefaultAsync();
        decimal markupFactor = 1.0m + (adminMarkupPercentage / 100.0m);
        int phaseIndex = 0;

        foreach (var phase in phases)
        {
            phaseIndex++;
            var categoryId = phase.CategoryId ?? defaultCategory?.Id ?? Guid.NewGuid();

            var jobPost = new JobPost
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                HomeownerProfileId = homeowner.HomeownerProfile.Id,
                ServiceCategoryId = categoryId,
                Title = phase.PhaseTitle,
                Description = $"Detailed work phase: {phase.PhaseTitle}",
                Location = location ?? "Sofia",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.JobPosts.Add(jobPost);

            if (categoryTradesmanMap != null && categoryTradesmanMap.Any())
            {
                Guid tradesmanId = Guid.Empty;
                if (phase.CategoryId.HasValue && categoryTradesmanMap.TryGetValue(phase.CategoryId.Value, out var tId))
                {
                    tradesmanId = tId;
                }
                else if (categoryTradesmanMap.TryGetValue(categoryId, out var cId))
                {
                    tradesmanId = cId;
                }
                else
                {
                    tradesmanId = categoryTradesmanMap.Values.FirstOrDefault(v => v != Guid.Empty);
                }


                if (tradesmanId != Guid.Empty)
                {
                    jobPost.AssignedTradesmanId = tradesmanId;
                    _context.CategoryTradesmanAssignments.Add(new CategoryTradesmanAssignment
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = project.Id,
                        JobPostId = jobPost.Id,
                        ServiceCategoryId = categoryId,
                        TradesmanId = tradesmanId,
                        AssignedByAdminId = homeownerUserId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            jobPost.Publish();

            int taskSeq = 0;
            foreach (var item in phase.Items)
            {
                taskSeq++;
                decimal tradesmanTotalEur = item.TotalEur;
                decimal homeownerTotalEur = Math.Round(tradesmanTotalEur * markupFactor, 2);


                var task = new JobTask
                {
                    Id = Guid.NewGuid(),
                    JobPostId = jobPost.Id,
                    Title = item.Title,
                    Description = string.IsNullOrWhiteSpace(item.Description) ? item.Title : item.Description,
                    SequenceOrder = taskSeq,
                    TradesmanPrice = tradesmanTotalEur,
                    EstimatedPrice = homeownerTotalEur,
                    Status = TaskStatus.ToDo,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Match existing SKU by SkuCode if present
                var matchingSku = await _context.ServiceSkus.FirstOrDefaultAsync(s => s.SkuCode == item.SkuCode);

                var skuItem = new TaskSkuItem
                {
                    Id = Guid.NewGuid(),
                    JobTaskId = task.Id,
                    ServiceSkuId = matchingSku?.Id ?? Guid.Empty,
                    Quantity = item.Quantity,
                    EstimatedPrice = tradesmanTotalEur,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                if (matchingSku != null)
                {
                    task.SkuItems.Add(skuItem);
                    _context.TaskSkuItems.Add(skuItem);
                }

                var criteria = new TaskAcceptanceCriteria
                {
                    Id = Guid.NewGuid(),
                    JobTaskId = task.Id,
                    Description = $"Quality verification for {item.Title} according to Bulgarian Construction Standards (BDS).",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                task.AcceptanceCriteria.Add(criteria);

                jobPost.JobTasks.Add(task);
                _context.JobTasks.Add(task);
                _context.TaskAcceptanceCriteria.Add(criteria);
            }
        }

        await _context.SaveChangesAsync();
        return project.Id;
    }

    public async Task AssignTradesmanToCategoryAsync(Guid projectId, Guid jobPostId, Guid tradesmanUserId, Guid adminUserId)
    {

        var jobPost = await _context.JobPosts
            .Include(j => j.Project)
            .FirstOrDefaultAsync(j => j.Id == jobPostId && j.ProjectId == projectId);

        if (jobPost == null)
            throw new ArgumentException("JobPost category container not found.");

        var tradesman = await _context.Users.FirstOrDefaultAsync(u => u.Id == tradesmanUserId);
        if (tradesman == null)
            throw new ArgumentException("Tradesman user account not found.");

        jobPost.AssignedTradesmanId = tradesman.Id;
        jobPost.UpdatedAt = DateTime.UtcNow;

        var existingAssignment = await _context.CategoryTradesmanAssignments
            .FirstOrDefaultAsync(a => a.ProjectId == projectId && a.JobPostId == jobPostId);

        if (existingAssignment != null)
        {
            existingAssignment.TradesmanId = tradesman.Id;
            existingAssignment.AssignedByAdminId = adminUserId;
            existingAssignment.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.CategoryTradesmanAssignments.Add(new CategoryTradesmanAssignment
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                JobPostId = jobPostId,
                ServiceCategoryId = jobPost.ServiceCategoryId,
                TradesmanId = tradesman.Id,
                AssignedByAdminId = adminUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        _context.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = tradesman.Id,
            Title = "New Trade Assignment",
            Message = $"You have been assigned as the primary tradesman for trade '{jobPost.Title}' on project '{jobPost.Project.Title}'.",
            IsRead = false,
            RelatedEntityId = projectId,
            RelatedEntityType = "Project",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    public async Task SetProjectStatusAsync(Guid projectId, ProjectStatus newStatus, Guid adminUserId)
    {
        var project = await _context.Projects
            .Include(p => p.Homeowner)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null)
            throw new ArgumentException("Project not found.");

        if (newStatus == ProjectStatus.Active)
        {
            project.Publish();
        }
        else if (newStatus == ProjectStatus.Completed)
        {
            project.Complete();
        }
        else if (newStatus == ProjectStatus.Archived)
        {
            project.Archive();
        }

        _context.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = project.HomeownerId,
            Title = "Project Status Updated",
            Message = $"Your project '{project.Title}' is now {newStatus}.",
            IsRead = false,
            RelatedEntityId = projectId,
            RelatedEntityType = "Project",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    public async Task UpdateTaskStatusAsync(Guid taskId, TaskStatus newStatus, Guid currentUserId, string? rejectionReason)
    {
        var task = await _context.JobTasks
            .Include(t => t.JobPost)
                .ThenThenProject()
            .Include(t => t.PaymentRecord)
            .Include(t => t.Comments)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
            throw new ArgumentException("Task not found.");

        var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);
        if (currentUser == null)
            throw new ArgumentException("User executing action not found.");

        bool isTradesman = currentUser.Role == UserRoleTypes.Tradesman;
        bool isHomeowner = currentUser.Role == UserRoleTypes.Homeowner;
        bool isAdmin = currentUser.Role == UserRoleTypes.Admin;

        if (isTradesman)
        {
            if (task.JobPost.AssignedTradesmanId != currentUserId)
            {
                throw new InvalidOperationException("Tradesman can only update status for their assigned tasks.");
            }

            if (newStatus == TaskStatus.Done)
            {
                throw new InvalidOperationException("Tradesmen are not permitted to mark tasks as Done. Only Homeowner or Admin can approve completion.");
            }
        }
        else if (isHomeowner && !isAdmin)
        {
            if (task.Status != TaskStatus.AwaitingApproval)
            {
                throw new InvalidOperationException("Homeowners cannot change task statuses directly. Tasks are updated by the assigned Tradesman. Homeowners can only approve or reject tasks submitted for completion approval.");
            }

            if (newStatus != TaskStatus.Done && newStatus != TaskStatus.InProgress)
            {
                throw new InvalidOperationException("Homeowners can only Approve (Mark Done) or Reject tasks awaiting completion approval.");
            }
        }

        if (newStatus == TaskStatus.InProgress)
        {
            if (task.Status == TaskStatus.AwaitingApproval && !string.IsNullOrWhiteSpace(rejectionReason))
            {
                var comment = task.Reject(currentUserId, rejectionReason);
                if (comment != null)
                {
                    _context.TaskComments.Add(comment);
                }
            }
            else
            {
                task.StartWork();
            }
        }
        else if (newStatus == TaskStatus.AwaitingApproval)
        {
            task.SubmitForApproval();

            _context.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = task.JobPost.Project.HomeownerId,
                Title = "Task Completion Awaiting Approval",
                Message = $"Task '{task.Title}' in category '{task.JobPost.Title}' has been submitted for completion approval by the tradesman.",
                IsRead = false,
                RelatedEntityId = task.JobPost.ProjectId,
                RelatedEntityType = "Project",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else if (newStatus == TaskStatus.Done)
        {
            if (!isHomeowner && !isAdmin)
            {
                throw new InvalidOperationException("Only the Homeowner or Admin can mark a task as Done.");
            }

            task.Approve();

            if (task.PaymentRecord != null)
            {
                if (_context.Entry(task.PaymentRecord).State == EntityState.Detached)
                {
                    _context.TaskPaymentRecords.Add(task.PaymentRecord);
                }

                if (task.PaymentRecord.Status == PaymentStatus.AwaitingPayment)
                {
                    task.PaymentRecord.CalculatedAmount = task.EstimatedPrice;
                    task.PaymentRecord.FinalAmount = task.EstimatedPrice;
                }
            }

            if (task.JobPost.AssignedTradesmanId.HasValue)
            {
                _context.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = task.JobPost.AssignedTradesmanId.Value,
                    Title = "Task Approved!",
                    Message = $"Task '{task.Title}' has been approved as completed and is now awaiting payment.",
                    IsRead = false,
                    RelatedEntityId = task.JobPost.ProjectId,
                    RelatedEntityType = "Project",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }
        else if (newStatus == TaskStatus.ToDo)
        {
            task.Status = TaskStatus.ToDo;
            task.UpdatedAt = DateTime.UtcNow;
        }

        if (newStatus != TaskStatus.Done && task.PaymentRecord != null && task.PaymentRecord.Status == PaymentStatus.AwaitingPayment)
        {
            _context.TaskPaymentRecords.Remove(task.PaymentRecord);
            task.PaymentRecord = null;
        }

        await _context.SaveChangesAsync();
    }

    public async Task AddTaskCommentAsync(Guid taskId, Guid authorId, string content, List<string>? imageUrls)
    {
        var task = await _context.JobTasks.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null)
            throw new ArgumentException("Task not found.");

        var author = await _context.Users.FirstOrDefaultAsync(u => u.Id == authorId);
        if (author == null)
            throw new ArgumentException("Author user not found.");

        var comment = new TaskComment
        {
            Id = Guid.NewGuid(),
            JobTaskId = taskId,
            AuthorId = authorId,
            Content = content,
            IsSystemNote = false,
            ImageUrls = imageUrls ?? new List<string>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TaskComments.Add(comment);
        await _context.SaveChangesAsync();
    }

    public async Task<ProjectKanbanBoardDto> GetProjectKanbanBoardAsync(Guid projectId, Guid currentUserId, UserRoleTypes role)
    {
        var project = await _context.Projects
            .AsNoTracking()
            .Include(p => p.Homeowner)
            .Include(p => p.JobPosts)
                .ThenInclude(j => j.ServiceCategory)
            .Include(p => p.JobPosts)
                .ThenInclude(j => j.AssignedTradesman)
            .Include(p => p.JobPosts)
                .ThenInclude(j => j.JobTasks)
                    .ThenInclude(t => t.SkuItems)
                        .ThenInclude(si => si.ServiceSku)
            .Include(p => p.JobPosts)
                .ThenInclude(j => j.JobTasks)
                    .ThenInclude(t => t.AcceptanceCriteria)
            .Include(p => p.JobPosts)
                .ThenInclude(j => j.JobTasks)
                    .ThenInclude(t => t.Comments)
                        .ThenInclude(c => c.Author)
            .Include(p => p.JobPosts)
                .ThenInclude(j => j.JobTasks)
                    .ThenInclude(t => t.PaymentRecord)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null)
            throw new ArgumentException("Project not found.");

        var board = new ProjectKanbanBoardDto
        {
            ProjectId = project.Id,
            ProjectTitle = project.Title,
            HomeownerId = project.HomeownerId,
            HomeownerName = $"{project.Homeowner.FirstName} {project.Homeowner.LastName}".Trim(),
            ProjectStatus = project.Status,
            AdminMarkupPercentage = project.AdminMarkupPercentage,
            CategorySections = new List<CategoryKanbanSectionDto>()
        };

        var filteredJobPosts = project.JobPosts;
        if (role == UserRoleTypes.Tradesman)
        {
            filteredJobPosts = project.JobPosts.Where(j => j.AssignedTradesmanId == currentUserId).ToList();
        }

        foreach (var jobPost in filteredJobPosts.OrderBy(j => j.Title))
        {
            var section = new CategoryKanbanSectionDto
            {
                JobPostId = jobPost.Id,
                ServiceCategoryId = jobPost.ServiceCategoryId,
                CategoryName = jobPost.ServiceCategory?.Name ?? jobPost.Title,
                AssignedTradesmanId = jobPost.AssignedTradesmanId,
                AssignedTradesmanName = jobPost.AssignedTradesman != null
                    ? $"{jobPost.AssignedTradesman.FirstName} {jobPost.AssignedTradesman.LastName}".Trim()
                    : "Unassigned",
                Tasks = new List<KanbanTaskCardDto>()
            };

            foreach (var task in jobPost.JobTasks.OrderBy(t => t.SequenceOrder))
            {
                var card = MapTaskToCard(task, jobPost, role, project.AdminMarkupPercentage);
                section.Tasks.Add(card);
            }

            board.CategorySections.Add(section);
        }

        return board;
    }

    public async Task<ProjectPaymentsBoardDto> GetProjectPaymentsBoardAsync(Guid projectId, Guid currentUserId, UserRoleTypes role)
    {
        var project = await _context.Projects
            .AsNoTracking()
            .Include(p => p.JobPosts)
                .ThenInclude(j => j.ServiceCategory)
            .Include(p => p.JobPosts)
                .ThenInclude(j => j.AssignedTradesman)
            .Include(p => p.JobPosts)
                .ThenInclude(j => j.JobTasks)
                    .ThenInclude(t => t.PaymentRecord)
                        .ThenInclude(pr => pr!.PaidByAdmin)
            .Include(p => p.JobPosts)
                .ThenInclude(j => j.JobTasks)
                    .ThenInclude(t => t.SkuItems)
                        .ThenInclude(si => si.ServiceSku)
            .Include(p => p.JobPosts)
                .ThenInclude(j => j.JobTasks)
                    .ThenInclude(t => t.Comments)
                        .ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null)
            throw new ArgumentException("Project not found.");

        var board = new ProjectPaymentsBoardDto
        {
            ProjectId = project.Id,
            ProjectTitle = project.Title,
            AdminMarkupPercentage = project.AdminMarkupPercentage,
            UpcomingTasks = new List<KanbanTaskCardDto>(),
            AwaitingPaymentTasks = new List<KanbanTaskCardDto>(),
            PaidTasks = new List<KanbanTaskCardDto>()
        };

        var filteredJobPosts = project.JobPosts;
        if (role == UserRoleTypes.Tradesman)
        {
            filteredJobPosts = project.JobPosts.Where(j => j.AssignedTradesmanId == currentUserId).ToList();
        }

        foreach (var jobPost in filteredJobPosts)
        {
            foreach (var task in jobPost.JobTasks)
            {
                var card = MapTaskToCard(task, jobPost, role, project.AdminMarkupPercentage);
                decimal priceToAccumulate = card.DisplayPrice;

                board.TotalTradesmanPayoutValue += card.TradesmanPrice;

                if (task.Status != TaskStatus.Done)
                {
                    board.UpcomingTasks.Add(card);
                    board.TotalUpcomingAmount += priceToAccumulate;
                }
                else if (task.PaymentRecord != null)
                {
                    decimal recAmount = role == UserRoleTypes.Tradesman
                        ? card.TradesmanPrice
                        : (task.PaymentRecord.FinalAmount > 0 ? task.PaymentRecord.FinalAmount : card.EstimatedPrice);

                    if (task.PaymentRecord.Status == PaymentStatus.AwaitingPayment)
                    {
                        board.AwaitingPaymentTasks.Add(card);
                        board.TotalAwaitingAmount += recAmount;
                    }
                    else if (task.PaymentRecord.Status == PaymentStatus.Paid)
                    {
                        board.PaidTasks.Add(card);
                        board.TotalPaidAmount += recAmount;
                    }
                }
                else
                {
                    board.AwaitingPaymentTasks.Add(card);
                    board.TotalAwaitingAmount += priceToAccumulate;
                }
            }
        }

        board.TotalProjectValue = board.TotalPaidAmount + board.TotalAwaitingAmount + board.TotalUpcomingAmount;
        board.NetAdminMarginValue = Math.Max(0, board.TotalProjectValue - board.TotalTradesmanPayoutValue);
        return board;
    }

    public async Task MarkTaskPaidAsync(Guid taskId, Guid adminUserId, decimal finalAmount, string? notes)
    {
        var task = await _context.JobTasks
            .Include(t => t.PaymentRecord)
            .Include(t => t.JobPost)
                .ThenInclude(j => j.Project)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
            throw new ArgumentException("Task not found.");

        var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminUserId && u.Role == UserRoleTypes.Admin);
        if (adminUser == null)
            throw new InvalidOperationException("Only an Admin user can mark payments as Paid.");

        if (task.PaymentRecord == null)
        {
            task.PaymentRecord = new TaskPaymentRecord
            {
                Id = Guid.NewGuid(),
                JobTaskId = task.Id,
                Status = PaymentStatus.AwaitingPayment,
                CalculatedAmount = task.EstimatedPrice,
                FinalAmount = finalAmount > 0 ? finalAmount : task.EstimatedPrice,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        task.PaymentRecord.MarkAsPaid(adminUserId, finalAmount > 0 ? finalAmount : task.PaymentRecord.CalculatedAmount, notes);

        if (task.JobPost.AssignedTradesmanId.HasValue)
        {
            _context.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = task.JobPost.AssignedTradesmanId.Value,
                Title = "Payment Disbursed",
                Message = $"Payment of €{task.PaymentRecord.FinalAmount:F2} for task '{task.Title}' has been processed and marked as Paid by Admin.",
                IsRead = false,
                RelatedEntityId = task.JobPost.ProjectId,
                RelatedEntityType = "Project",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        _context.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = task.JobPost.Project.HomeownerId,
            Title = "Task Payment Verified",
            Message = $"Payment of €{task.PaymentRecord.FinalAmount:F2} for task '{task.Title}' has been marked as Paid by Admin.",
            IsRead = false,
            RelatedEntityId = task.JobPost.ProjectId,
            RelatedEntityType = "Project",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    private KanbanTaskCardDto MapTaskToCard(JobTask task, JobPost jobPost, UserRoleTypes role, decimal markupPercentage)
    {
        decimal markupFactor = 1.0m + (markupPercentage / 100.0m);

        decimal tradesmanTaskPrice = task.TradesmanPrice > 0
            ? task.TradesmanPrice
            : (task.SkuItems.Any()
                ? Math.Round(task.SkuItems.Sum(si => Math.Round((si.ServiceSku?.BasePrice ?? 0) / 1.95583m * si.Quantity, 2)), 2)
                : Math.Round(task.EstimatedPrice > 0 ? task.EstimatedPrice / markupFactor : 0m, 2));

        decimal homeownerTaskPrice = Math.Round(tradesmanTaskPrice * markupFactor, 2);

        decimal displayPrice = role == UserRoleTypes.Tradesman ? tradesmanTaskPrice : homeownerTaskPrice;

        return new KanbanTaskCardDto
        {
            TaskId = task.Id,
            JobPostId = jobPost.Id,
            CategoryName = jobPost.ServiceCategory?.Name ?? jobPost.Title,
            Title = task.Title,
            Description = task.Description,
            SequenceOrder = task.SequenceOrder,
            EstimatedPrice = homeownerTaskPrice,
            TradesmanPrice = tradesmanTaskPrice,
            DisplayPrice = displayPrice,
            Status = task.Status,
            AssignedTradesmanId = jobPost.AssignedTradesmanId,
            AssignedTradesmanName = jobPost.AssignedTradesman != null
                ? $"{jobPost.AssignedTradesman.FirstName} {jobPost.AssignedTradesman.LastName}".Trim()
                : "Unassigned",
            SkuCount = task.SkuItems.Count,
            CommentCount = task.Comments.Count,
            Skus = task.SkuItems.Select(si =>
            {
                decimal tradesmanSubtotal = Math.Round((si.ServiceSku?.BasePrice ?? 0) / 1.95583m * si.Quantity, 2);
                decimal homeownerSubtotal = Math.Round(tradesmanSubtotal * markupFactor, 2);
                return new TaskSkuSummaryDto
                {
                    SkuId = si.ServiceSkuId,
                    SkuCode = si.ServiceSku?.SkuCode ?? "SKU",
                    SkuTitle = si.ServiceSku?.Name ?? "Service Item",
                    Quantity = si.Quantity,
                    Unit = si.ServiceSku?.UnitType ?? "Flat",
                    BasePriceBgn = si.ServiceSku?.BasePrice ?? 0,
                    TradesmanSubtotalEur = tradesmanSubtotal,
                    SubtotalEur = role == UserRoleTypes.Tradesman ? tradesmanSubtotal : homeownerSubtotal
                };
            }).ToList(),
            AcceptanceCriteria = task.AcceptanceCriteria.Select(c => new TaskAcceptanceCriteriaDto
            {
                Id = c.Id,
                Description = c.Description
            }).ToList(),
            Comments = task.Comments.OrderBy(c => c.CreatedAt).Select(c => new TaskCommentDto
            {
                Id = c.Id,
                AuthorId = c.AuthorId,
                AuthorName = c.Author != null ? $"{c.Author.FirstName} {c.Author.LastName}".Trim() : "System User",
                AuthorRole = c.Author?.Role ?? UserRoleTypes.Admin,
                Content = c.Content,
                IsSystemNote = c.IsSystemNote,
                ImageUrls = c.ImageUrls,
                CreatedAt = c.CreatedAt
            }).ToList(),
            PaymentInfo = task.PaymentRecord != null ? new TaskPaymentInfoDto
            {
                PaymentRecordId = task.PaymentRecord.Id,
                Status = task.PaymentRecord.Status,
                CalculatedAmount = task.PaymentRecord.CalculatedAmount,
                FinalAmount = task.PaymentRecord.FinalAmount,
                PaidAt = task.PaymentRecord.PaidAt,
                PaidByAdminName = task.PaymentRecord.PaidByAdmin != null
                    ? $"{task.PaymentRecord.PaidByAdmin.FirstName} {task.PaymentRecord.PaidByAdmin.LastName}".Trim()
                    : null,
                PaymentNotes = task.PaymentRecord.PaymentNotes
            } : null
        };
    }

    public async Task<List<ServiceCategory>> GetTradeCategoriesAsync()
    {
        return await _context.ServiceCategories.AsNoTracking().ToListAsync();
    }

    public async Task DeleteTaskAsync(Guid taskId, Guid currentUserId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);
        if (user == null || user.Role != UserRoleTypes.Admin)
            throw new InvalidOperationException("Only Admin can delete tasks.");

        var task = await _context.JobTasks
            .Include(t => t.PaymentRecord)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task != null)
        {
            if (task.PaymentRecord != null)
            {
                _context.TaskPaymentRecords.Remove(task.PaymentRecord);
            }
            _context.JobTasks.Remove(task);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Guid> CreateTaskAsync(Guid jobPostId, string title, string description, decimal estimatedPrice, Guid adminUserId)
    {
        var admin = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminUserId && u.Role == UserRoleTypes.Admin);
        if (admin == null)
            throw new InvalidOperationException("Only an Admin can create new tasks.");

        var jobPost = await _context.JobPosts.Include(j => j.JobTasks).FirstOrDefaultAsync(j => j.Id == jobPostId);
        if (jobPost == null)
            throw new ArgumentException("Job section not found.");

        int maxSeq = jobPost.JobTasks.Any() ? jobPost.JobTasks.Max(t => t.SequenceOrder) : 0;

        var task = new JobTask
        {
            Id = Guid.NewGuid(),
            JobPostId = jobPostId,
            Title = title,
            Description = description,
            SequenceOrder = maxSeq + 1,
            EstimatedPrice = estimatedPrice > 0 ? estimatedPrice : 0,
            TradesmanPrice = estimatedPrice > 0 ? estimatedPrice : 0,
            Status = TaskStatus.ToDo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.JobTasks.Add(task);
        await _context.SaveChangesAsync();
        return task.Id;
    }

    public async Task UpdateTaskDetailsAndSkusAsync(Guid taskId, string title, string description, decimal estimatedPrice, List<TaskSkuEditDto> skus, Guid adminUserId)
    {
        var admin = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminUserId && u.Role == UserRoleTypes.Admin);
        if (admin == null)
            throw new InvalidOperationException("Only an Admin can modify task details and SKUs.");

        var task = await _context.JobTasks
            .Include(t => t.JobPost)
                .ThenInclude(j => j.Project)
            .Include(t => t.PaymentRecord)
            .Include(t => t.SkuItems)
                .ThenInclude(si => si.ServiceSku)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
            throw new ArgumentException("Task not found.");

        var inputSkuIds = skus.Where(s => s.SkuId.HasValue).Select(s => s.SkuId!.Value).ToHashSet();

        var toRemove = task.SkuItems.Where(si => !inputSkuIds.Contains(si.Id)).ToList();
        foreach (var item in toRemove)
        {
            _context.TaskSkuItems.Remove(item);
        }

        foreach (var skuDto in skus)
        {
            if (skuDto.SkuId.HasValue)
            {
                var existing = task.SkuItems.FirstOrDefault(si => si.Id == skuDto.SkuId.Value);
                if (existing != null)
                {
                    existing.Quantity = skuDto.Quantity;
                    existing.EstimatedPrice = Math.Round(skuDto.BasePriceBgn / 1.95583m * skuDto.Quantity, 2);
                    existing.UpdatedAt = DateTime.UtcNow;

                    if (existing.ServiceSku != null)
                    {
                        existing.ServiceSku.SkuCode = string.IsNullOrWhiteSpace(skuDto.SkuCode) ? existing.ServiceSku.SkuCode : skuDto.SkuCode;
                        existing.ServiceSku.Name = string.IsNullOrWhiteSpace(skuDto.SkuTitle) ? existing.ServiceSku.Name : skuDto.SkuTitle;
                        existing.ServiceSku.UnitType = string.IsNullOrWhiteSpace(skuDto.Unit) ? existing.ServiceSku.UnitType : skuDto.Unit;
                        existing.ServiceSku.BasePrice = skuDto.BasePriceBgn;
                    }
                }
            }
            else
            {
                var serviceSku = new ServiceSku
                {
                    Id = Guid.NewGuid(),
                    ServiceCategoryId = task.JobPost.ServiceCategoryId,
                    SkuCode = string.IsNullOrWhiteSpace(skuDto.SkuCode) ? $"SKU-{Guid.NewGuid().ToString()[..6].ToUpper()}" : skuDto.SkuCode,
                    Name = string.IsNullOrWhiteSpace(skuDto.SkuTitle) ? "Custom Task SKU" : skuDto.SkuTitle,
                    Description = skuDto.SkuTitle,
                    BasePrice = skuDto.BasePriceBgn,
                    UnitType = string.IsNullOrWhiteSpace(skuDto.Unit) ? "m²" : skuDto.Unit,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.ServiceSkus.Add(serviceSku);

                var newItem = new TaskSkuItem
                {
                    Id = Guid.NewGuid(),
                    JobTaskId = task.Id,
                    ServiceSkuId = serviceSku.Id,
                    Quantity = skuDto.Quantity,
                    EstimatedPrice = Math.Round(skuDto.BasePriceBgn / 1.95583m * skuDto.Quantity, 2),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.TaskSkuItems.Add(newItem);
                task.SkuItems.Add(newItem);
            }
        }

        decimal tradesmanSubtotal = skus.Any()
            ? Math.Round(skus.Sum(s => (s.BasePriceBgn / 1.95583m) * s.Quantity), 2)
            : (estimatedPrice > 0 ? estimatedPrice : task.TradesmanPrice);

        decimal markupFactor = 1.0m + ((task.JobPost?.Project?.AdminMarkupPercentage ?? 20.0m) / 100.0m);
        decimal homeownerTotal = Math.Round(tradesmanSubtotal * markupFactor, 2);

        task.Title = title.Trim();
        task.Description = description.Trim();
        task.TradesmanPrice = tradesmanSubtotal;
        task.EstimatedPrice = homeownerTotal;
        task.UpdatedAt = DateTime.UtcNow;

        if (task.PaymentRecord != null && task.PaymentRecord.Status == PaymentStatus.AwaitingPayment)
        {
            task.PaymentRecord.CalculatedAmount = homeownerTotal;
            task.PaymentRecord.FinalAmount = homeownerTotal;
            task.PaymentRecord.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task UpdateProjectMarkupPercentageAsync(Guid projectId, decimal markupPercentage, Guid adminUserId)
    {
        var admin = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminUserId && u.Role == UserRoleTypes.Admin);
        if (admin == null)
            throw new InvalidOperationException("Only an Admin can modify project markup percentages.");

        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
        if (project == null)
            throw new ArgumentException("Project not found.");

        project.AdminMarkupPercentage = markupPercentage < 0 ? 0 : markupPercentage;
        await _context.SaveChangesAsync();
    }
}

internal static class NavigationExtensions
{
    public static Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<JobTask, Project> ThenThenProject(
        this Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<JobTask, JobPost> queryable)
    {
        return queryable.ThenInclude(j => j.Project);
    }
}
