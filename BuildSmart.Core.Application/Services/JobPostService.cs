using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using BuildSmart.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace BuildSmart.Core.Application.Services;

public class JobPostService : IJobPostService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IScopeGenerationQueue _scopeGenerationQueue;
    private readonly INotificationService _notificationService;
    private readonly IJobsNotificationService _jobsNotificationService;

    public JobPostService(
        IUnitOfWork unitOfWork, 
        IScopeGenerationQueue scopeGenerationQueue,
        INotificationService notificationService,
        IJobsNotificationService jobsNotificationService)
    {
        _unitOfWork = unitOfWork;
        _scopeGenerationQueue = scopeGenerationQueue;
        _notificationService = notificationService;
        _jobsNotificationService = jobsNotificationService;
    }

    public async Task SubmitJobForScopeGenerationAsync(Guid jobPostId)
    {
        var jobPost = await _unitOfWork.JobPosts.GetByIdAsync(jobPostId)
            ?? throw new ArgumentException("Job post not found");

        if (jobPost.Status != JobPostStatus.Draft && 
            jobPost.Status != JobPostStatus.Rejected &&
            jobPost.Status != JobPostStatus.WaitingForUserReview)
        {
            throw new InvalidOperationException("AI generation can only be triggered for jobs in Draft, Rejected, or WaitingForUserReview status.");
        }

        jobPost.SubmitForScopeGeneration();
        
        // Reset feedback when submitting a fix
        jobPost.AdminFeedback = null;

        _unitOfWork.JobPosts.Update(jobPost);

        // Sync Project Status
        var project = await _unitOfWork.Projects.GetByIdAsync(jobPost.ProjectId);
        if (project != null && project.Status != ProjectStatus.UnderReview)
        {
            project.SubmitForReview();
            _unitOfWork.Projects.Update(project);
        }

        await _unitOfWork.SaveChangesAsync();

        // Queue for background processing
        await _scopeGenerationQueue.QueueBackgroundWorkItemAsync(jobPost.Id, CancellationToken.None);
    }

    public async Task ApproveJobScopeAsync(Guid jobPostId, string finalScope)
    {
        var jobPost = await _unitOfWork.JobPosts.GetByIdAsync(jobPostId)
            ?? throw new ArgumentException("Job post not found");

        if (jobPost.Status == JobPostStatus.WaitingForAdminReview)
        {
            // Idempotency: If already waiting for admin, allow updating the text without throwing
            jobPost.UserEditedScope = finalScope;
            jobPost.Description = finalScope;
            jobPost.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            jobPost.ApproveScope(finalScope);
        }

        _unitOfWork.JobPosts.Update(jobPost);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task AdminReviewJobScopeAsync(Guid jobPostId, bool approved, string? feedback, Guid? reviewerId)
    {
        var jobPost = await _unitOfWork.JobPosts.GetByIdAsync(jobPostId)
            ?? throw new ArgumentException("Job post not found");

        if (approved)
        {
            if (jobPost.Status == JobPostStatus.Open)
            {
                // Idempotent: Already approved
                jobPost.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                jobPost.AdminApproveScope();
                jobPost.Publish();
            }

            // Notify Homeowner of Approval
            await _notificationService.SendNotificationAsync(
                jobPost.Project.HomeownerId,
                "Scope Approved",
                $"Admin has approved the scope for '{jobPost.Title}'. It is now live!",
                jobPost.Id,
                "JobPost"
            );
        }
        else
        {
            var rejectionReason = feedback ?? "Rejected without feedback.";
            if (jobPost.Status == JobPostStatus.Rejected)
            {
                // Idempotent: Already rejected, just update feedback
                jobPost.AdminFeedback = rejectionReason;
                jobPost.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                jobPost.AdminRejectScope(rejectionReason);
            }

            // Create a feedback record so it shows in the threaded discussion
            if (reviewerId.HasValue)
            {
                await AddFeedbackAsync(jobPost.Id, reviewerId.Value, $"[REJECTION] {rejectionReason}");
            }

            // Notify Homeowner of Rejection
            await _notificationService.SendNotificationAsync(
                jobPost.Project.HomeownerId,
                "Scope Rejected",
                $"Admin has requested changes for '{jobPost.Title}'. Please check the feedback.",
                jobPost.Id,
                "JobPost"
            );
        }

        _unitOfWork.JobPosts.Update(jobPost);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<Project> CreateProjectAsync(Guid homeownerId, string title, string description)
    {
        var project = new Project
        {
            HomeownerId = homeownerId,
            Title = title,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Projects.AddAsync(project);
        await _unitOfWork.SaveChangesAsync();

        return project;
    }

    public async Task<JobPost> AddJobToProjectAsync(
        Guid projectId, 
        Guid categoryId, 
        string title, 
        string jobDetailsJson, 
        string location,
        Amount? estimatedBudget,
        List<string> imageUrls)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(projectId)
            ?? throw new ArgumentException("Project not found");

        var category = await _unitOfWork.ServiceCategories.GetByIdAsync(categoryId)
            ?? throw new ArgumentException("Category not found");

        // Note: We need to handle the HomeownerProfileId correctly.
        // Usually, a User has a HomeownerProfile.
        var homeowner = await _unitOfWork.Users.GetByIdAsync(project.HomeownerId);
        if (homeowner?.HomeownerProfile == null)
        {
             throw new InvalidOperationException("User does not have a Homeowner profile. Please complete your profile first.");
        }

        var jobPost = new JobPost
        {
            ProjectId = projectId,
            ServiceCategoryId = categoryId,
            HomeownerProfileId = homeowner.HomeownerProfile.Id, // Set correct Profile ID
            Title = title,
            JobDetails = jobDetailsJson,
            Description = $"Job for {category.Name}: {title}", // Placeholder, ideally generated from Wizard
            Location = location,
            EstimatedBudget = estimatedBudget,
            ImageUrls = imageUrls,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.JobPosts.AddAsync(jobPost);
        await _unitOfWork.SaveChangesAsync();

        return jobPost;
    }

    public async Task UpdateJobScopeAsync(Guid jobPostId, string newDetailsJson, string newDescription)
    {
        var jobPost = await _unitOfWork.JobPosts.GetByIdAsync(jobPostId)
            ?? throw new ArgumentException("Job post not found");

        jobPost.UpdateScope(newDetailsJson, newDescription);
        
        _unitOfWork.JobPosts.Update(jobPost);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task SaveDraftAsync(Guid jobPostId, string jobDetailsJson, string? description, string? location, Amount? estimatedBudget)
    {
        var jobPost = await _unitOfWork.JobPosts.GetByIdAsync(jobPostId)
            ?? throw new ArgumentException("Job post not found");

        if (jobPost.Status != JobPostStatus.Draft)
        {
             // We allow saving draft only if it is already draft? 
             // Or maybe we allow editing active jobs back to draft? 
             // For now, let's assume this is for the creation wizard.
             // If it's already published, use UpdateScope (Amendment).
        }

        jobPost.JobDetails = jobDetailsJson;
        if (!string.IsNullOrEmpty(description)) jobPost.Description = description;
        if (!string.IsNullOrEmpty(location)) jobPost.Location = location;
        if (estimatedBudget != null) jobPost.EstimatedBudget = estimatedBudget;
        
        // Ensure status is Draft (in case it was something else, though usually it stays Draft)
        // jobPost.Status = JobPostStatus.Draft; // Setter is private, need method or reflection?
        // Ideally we have a method RevertToDraft() or similar if needed, 
        // but typically we just update the fields.
        
        jobPost.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.JobPosts.Update(jobPost);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task SubmitJobPostAsync(Guid jobPostId)
    {
        var jobPost = await _unitOfWork.JobPosts.GetByIdAsync(jobPostId)
            ?? throw new ArgumentException("Job post not found");
        
        // 1. Get Categories (Specific + Global)
        var specificCategory = await _unitOfWork.ServiceCategories.GetByIdAsync(jobPost.ServiceCategoryId);
        var globalCategories = await _unitOfWork.ServiceCategories.GetQueryable().Where(c => c.IsGlobal && c.Status == CategoryStatus.Active).ToListAsync();

        var allCategories = new List<ServiceCategory>();
        if (specificCategory != null) allCategories.Add(specificCategory);
        allCategories.AddRange(globalCategories);

        // 2. Validate Answers
        ValidateMandatoryQuestions(allCategories, jobPost.JobDetails);

        // 3. Trigger Scope Generation (AI)
        jobPost.SubmitForScopeGeneration();
        
        // 4. Also update Project status if needed
        if (jobPost.ProjectId != Guid.Empty)
        {
             var project = await _unitOfWork.Projects.GetByIdAsync(jobPost.ProjectId);
             if (project != null && project.Status == ProjectStatus.Draft)
             {
                 project.SubmitForReview();
                 _unitOfWork.Projects.Update(project);
             }
        }

        _unitOfWork.JobPosts.Update(jobPost);
        await _unitOfWork.SaveChangesAsync();

        // Notify Admins of New Submission
        var admins = await _unitOfWork.Users.GetQueryable().Where(u => u.Role == UserRoleTypes.Admin).ToListAsync();
        foreach (var admin in admins)
        {
            await _notificationService.SendNotificationAsync(
                admin.Id,
                "New Job Submission",
                $"A new scope for '{jobPost.Title}' is ready for admin review.",
                jobPost.Id,
                "JobPost"
            );
        }

        // Queue for background processing
        await _scopeGenerationQueue.QueueBackgroundWorkItemAsync(jobPost.Id, CancellationToken.None);
    }

    private void ValidateMandatoryQuestions(List<ServiceCategory> categories, string jobDetailsJson)
    {
        // Parse Answers
        using var doc = System.Text.Json.JsonDocument.Parse(jobDetailsJson);
        var root = doc.RootElement;

        foreach (var cat in categories)
        {
            if (string.IsNullOrEmpty(cat.TemplateStructure) || cat.TemplateStructure == "{}") continue;

            try 
            {
                using var templateDoc = System.Text.Json.JsonDocument.Parse(cat.TemplateStructure);
                if (templateDoc.RootElement.TryGetProperty("questions", out var questionsElement))
                {
                    foreach (var q in questionsElement.EnumerateArray())
                    {
                        // Check if required
                        bool isRequired = false;
                        if (q.TryGetProperty("isRequired", out var reqProp))
                        {
                            isRequired = reqProp.GetBoolean();
                        }

                        if (isRequired)
                        {
                            var qId = q.GetProperty("id").GetString();
                            if (string.IsNullOrEmpty(qId)) continue;

                            // Check if answer exists and is not empty
                            if (!root.TryGetProperty(qId, out var answerProp) || 
                                (answerProp.ValueKind == System.Text.Json.JsonValueKind.String && string.IsNullOrWhiteSpace(answerProp.GetString())) ||
                                answerProp.ValueKind == System.Text.Json.JsonValueKind.Null)
                            {
                                var qText = q.TryGetProperty("text", out var textProp) ? textProp.GetString() : "Unknown Question";
                                throw new InvalidOperationException($"Missing mandatory answer for: {qText}");
                            }
                        }
                    }
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // Ignore malformed templates for now, or log warning
            }
        }
    }

    public async Task<Bid> SubmitBidAsync(Guid tradesmanProfileId, Guid jobPostId, Amount amount, string? comment)
    {
        var jobPost = await _unitOfWork.JobPosts.GetByIdAsync(jobPostId)
            ?? throw new ArgumentException("Job post not found");

        var bid = new Bid
        {
            JobPostId = jobPostId,
            TradesmanProfileId = tradesmanProfileId,
            Amount = amount,
            Comment = comment,
            LinkedAmendmentVersion = jobPost.AmendmentCount,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Bids.AddAsync(bid);
        await _unitOfWork.SaveChangesAsync();

        // Notify Homeowner of New Bid
        var project = await _unitOfWork.Projects.GetByIdAsync(jobPost.ProjectId);
        if (project != null)
        {
            await _notificationService.SendNotificationAsync(
                project.HomeownerId,
                "New Bid Received",
                $"A tradesman has submitted a bid for '{jobPost.Title}'.",
                bid.Id,
                "Bid"
            );
        }

        return bid;
    }

    public async Task PassAuctionAsync(Guid tradesmanProfileId, Guid jobPostId)
    {
        var existingAction = await _unitOfWork.AuctionActions.GetQueryable()
            .FirstOrDefaultAsync(a => a.TradesmanProfileId == tradesmanProfileId && a.JobPostId == jobPostId && a.ActionType == AuctionActionType.Passed);

        if (existingAction != null) return; // Already passed

        var action = new TradesmanAuctionAction
        {
            TradesmanProfileId = tradesmanProfileId,
            JobPostId = jobPostId,
            ActionType = AuctionActionType.Passed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.AuctionActions.AddAsync(action);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<Booking> AcceptBidAsync(Guid bidId)
    {
        var bid = await _unitOfWork.Bids.GetByIdAsync(bidId)
            ?? throw new ArgumentException("Bid not found");

        if (bid.IsOutdatedVersion(bid.JobPost.AmendmentCount))
        {
            throw new InvalidOperationException("Cannot accept an outdated bid.");
        }

        bid.Accept();

        // Calculate Fees (10% each side)
        var feeRate = 0.10m;
        var feeAmountValue = bid.Amount.Total * feeRate;
        
        var feeHomeowner = Amount.Create(bid.Amount.Currency, feeAmountValue, 0);
        var feeTradesman = Amount.Create(bid.Amount.Currency, feeAmountValue, 0);
        var totalEscrow = Amount.Create(bid.Amount.Currency, bid.Amount.Total + feeAmountValue, 0);

        var booking = new Booking
        {
            HomeownerId = bid.JobPost.Project.HomeownerId,
            TradesmanProfileId = bid.TradesmanProfileId,
            AgreedBidAmount = bid.Amount,
            PlatformFeeHomeowner = feeHomeowner,
            PlatformFeeTradesman = feeTradesman,
            TotalEscrowAmount = totalEscrow,
            JobDescription = bid.JobPost.Description,
            RequestedDate = DateTime.UtcNow, // Or from project?
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Bookings.AddAsync(booking);
        
        // Close bidding on job post
        bid.JobPost.CloseBidding();
        
        await _unitOfWork.SaveChangesAsync();

        // Notify Tradesman
        await _notificationService.SendNotificationAsync(
            bid.TradesmanProfile.UserId,
            "Bid Accepted!",
            $"Your bid for '{bid.JobPost.Title}' has been accepted. Check your bookings.",
            booking.Id,
            "Booking"
        );

        return booking;
    }

    public async Task<JobPostFeedback> AddFeedbackAsync(Guid jobPostId, Guid authorId, string text)
    {
        var jobPost = await _unitOfWork.JobPosts.GetByIdAsync(jobPostId)
            ?? throw new ArgumentException("Job post not found");

        var user = await _unitOfWork.Users.GetByIdAsync(authorId);
        var feedback = new JobPostFeedback
        {
            JobPostId = jobPostId,
            AuthorId = authorId,
            Text = text,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.JobPostFeedbacks.AddAsync(feedback);
        
        // Ensure Author is loaded for the response
        if (user != null) feedback.Author = user;

        // Logic: If Homeowner responds to a Rejected job, move it back to Review
        if (user != null && user.Role == UserRoleTypes.Homeowner && jobPost.Status == JobPostStatus.Rejected)
        {
            jobPost.ResubmitAfterClarification();
            _unitOfWork.JobPosts.Update(jobPost);

            // Sync Project Status
            var project = await _unitOfWork.Projects.GetByIdAsync(jobPost.ProjectId);
            if (project != null && project.Status != ProjectStatus.UnderReview)
            {
                project.SubmitForReview();
                _unitOfWork.Projects.Update(project);
            }

            // Notify Admins
            var admins = await _unitOfWork.Users.GetQueryable().Where(u => u.Role == UserRoleTypes.Admin).ToListAsync();
            foreach (var admin in admins)
            {
                await _notificationService.SendNotificationAsync(
                    admin.Id,
                    "New Clarification",
                    $"{user.FirstName} responded to feedback on '{jobPost.Title}'.",
                    jobPost.Id,
                    "JobPost"
                );
            }
        }
        else if (user != null && user.Role == UserRoleTypes.Admin)
        {
            // Logic: If Admin responds, notify the Homeowner
            var project = await _unitOfWork.Projects.GetByIdAsync(jobPost.ProjectId);
            
            // AUTOMATIC STATUS TRANSITION: Move back to User Review so buttons show up
            jobPost.RequestUserReview();
            _unitOfWork.JobPosts.Update(jobPost);

            if (project != null)
            {
                // Ensure project is not 'Rejected' if admin is asking questions
                if (project.Status == ProjectStatus.UnderReview)
                {
                    // Already correct
                }

                await _notificationService.SendNotificationAsync(
                    project.HomeownerId,
                    "Admin Clarification",
                    $"An admin has asked for clarification on '{jobPost.Title}'.",
                    jobPost.Id,
                    "JobPost"
                );
            }
        }

        await _unitOfWork.SaveChangesAsync();
        return feedback;
    }

    public async Task<JobPostFeedback> ReplyToFeedbackAsync(Guid parentFeedbackId, Guid userId, string replyText)
    {
        var parentFeedback = await _unitOfWork.JobPostFeedbacks.GetByIdAsync(parentFeedbackId)
            ?? throw new ArgumentException("Parent feedback not found");

        var user = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new ArgumentException("User not found");

        var reply = new JobPostFeedback
        {
            JobPostId = parentFeedback.JobPostId,
            ParentFeedbackId = parentFeedbackId,
            AuthorId = userId,
            Text = replyText,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.JobPostFeedbacks.AddAsync(reply);
        
        // Notification Logic
        var parentAuthorId = parentFeedback.AuthorId;
        if (parentAuthorId != userId)
        {
            var jobPost = await _unitOfWork.JobPosts.GetByIdAsync(parentFeedback.JobPostId);
            await _notificationService.SendNotificationAsync(
                parentAuthorId,
                "New Reply",
                $"Someone replied to your feedback on job '{jobPost?.Title ?? "Feedback"}'.",
                parentFeedback.JobPostId,
                "JobPost"
            );
        }

        await _unitOfWork.SaveChangesAsync();
        reply.Author = user; // Ensure author is populated for result
        return reply;
    }

    public async Task<JobPostFeedback> ResolveFeedbackAsync(Guid feedbackId)
    {
        var feedback = await _unitOfWork.JobPostFeedbacks.GetByIdAsync(feedbackId)
            ?? throw new ArgumentException("Feedback not found");

        feedback.IsResolved = true;
        feedback.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.JobPostFeedbacks.Update(feedback);
        await _unitOfWork.SaveChangesAsync();
        return feedback;
    }

    public async Task<bool> AddAdminQuestionAsync(Guid jobPostId, string questionText, string type, bool isRequired, List<string>? options = null)
    {
        var jobPost = await _unitOfWork.JobPosts.GetByIdAsync(jobPostId)
            ?? throw new ArgumentException("Job post not found");

        // Parse existing or create new questions list
        var questions = new List<object>();
        if (!string.IsNullOrEmpty(jobPost.AdditionalQuestionsJson))
        {
            var existing = System.Text.Json.JsonSerializer.Deserialize<List<System.Text.Json.Nodes.JsonObject>>(jobPost.AdditionalQuestionsJson);
            if (existing != null) questions.AddRange(existing);
        }

        // Add new question using standard template format
        var newQuestion = new 
        {
            id = Guid.NewGuid().ToString(), // Unique ID for answering
            text = questionText,
            type = type,
            required = isRequired,
            options = options ?? new List<string>()
        };
        questions.Add(newQuestion);

        jobPost.AdditionalQuestionsJson = System.Text.Json.JsonSerializer.Serialize(questions);
        
        // AUTOMATIC STATUS TRANSITION: Move back to User Review so homeowner can answer
        jobPost.RequestUserReview();

        _unitOfWork.JobPosts.Update(jobPost);
        await _unitOfWork.SaveChangesAsync();

        // Notify Homeowner
        await _notificationService.SendNotificationAsync(
            jobPost.Project.HomeownerId,
            "Action Required",
            $"An admin has added a specific question for '{jobPost.Title}'. Please update your answers.",
            jobPost.Id,
            "JobPost"
        );

        return true;
    }

    public async Task<JobPostQuestion> AskJobQuestionAsync(Guid tradesmanProfileId, Guid jobPostId, string questionText)
    {
        var jobPost = await _unitOfWork.JobPosts.GetByIdAsync(jobPostId)
            ?? throw new ArgumentException("Job post not found");

        var question = new JobPostQuestion
        {
            JobPostId = jobPostId,
            TradesmanProfileId = tradesmanProfileId,
            QuestionText = questionText,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.JobPostQuestions.AddAsync(question);
        await _unitOfWork.SaveChangesAsync();

        // Notify Homeowner
        var project = await _unitOfWork.Projects.GetByIdAsync(jobPost.ProjectId);
        if (project != null)
        {
            await _notificationService.SendNotificationAsync(
                project.HomeownerId,
                "New Auction Question",
                $"A tradesman asked a question about '{jobPost.Title}'.",
                jobPost.Id,
                "AuctionQuestion"
            );
        }

        return question;
    }

    public async Task<JobPostQuestion> AnswerJobQuestionAsync(Guid questionId, string answerText)
    {
        var question = await _unitOfWork.JobPostQuestions.GetByIdAsync(questionId)
            ?? throw new ArgumentException("Question not found");

        question.Answer(answerText);
        
        _unitOfWork.JobPostQuestions.Update(question);
        await _unitOfWork.SaveChangesAsync();

        // Notify Tradesman
        if (question.TradesmanProfileId.HasValue)
        {
            var tradesman = await _unitOfWork.TradesmanProfiles.GetByIdAsync(question.TradesmanProfileId.Value);
            if (tradesman != null)
            {
                await _notificationService.SendNotificationAsync(
                    tradesman.UserId,
                    "Question Answered",
                    $"Your question has been answered for job '{question.JobPost?.Title ?? "Auction"}'.",
                    question.JobPostId,
                    "AuctionAnswer"
                );
            }
        }
        else if (question.AuthorId.HasValue)
        {
            var author = await _unitOfWork.Users.GetByIdAsync(question.AuthorId.Value);
            if (author != null)
            {
                await _notificationService.SendNotificationAsync(
                    author.Id,
                    "Question Answered",
                    $"Your question has been answered for job '{question.JobPost?.Title ?? "Auction"}'.",
                    question.JobPostId,
                    "AuctionAnswer"
                );
            }
        }

        return question;
    }

    public async Task<JobPostQuestion> ReplyToQuestionAsync(Guid parentQuestionId, Guid userId, string replyText)
    {
        var parentQuestion = await _unitOfWork.JobPostQuestions.GetByIdAsync(parentQuestionId)
            ?? throw new ArgumentException("Parent question not found");

        var user = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new ArgumentException("User not found");

        var reply = new JobPostQuestion
        {
            JobPostId = parentQuestion.JobPostId,
            ParentQuestionId = parentQuestionId,
            AuthorId = userId,
            QuestionText = replyText,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (user.TradesmanProfile != null)
        {
            reply.TradesmanProfileId = user.TradesmanProfile.Id;
        }

        await _unitOfWork.JobPostQuestions.AddAsync(reply);
        await _unitOfWork.SaveChangesAsync();

        // Notification Logic
        var parentAuthorId = parentQuestion.AuthorId;
        if (!parentAuthorId.HasValue && parentQuestion.TradesmanProfileId.HasValue)
        {
            var pTradesman = await _unitOfWork.TradesmanProfiles.GetByIdAsync(parentQuestion.TradesmanProfileId.Value);
            if (pTradesman != null) parentAuthorId = pTradesman.UserId;
        }

        if (parentAuthorId.HasValue && parentAuthorId.Value != userId)
        {
            await _notificationService.SendNotificationAsync(
                parentAuthorId.Value,
                "New Reply",
                $"Someone replied to your comment on job '{parentQuestion.JobPost?.Title ?? "Auction"}'.",
                parentQuestion.JobPostId,
                "AuctionAnswer"
            );
        }
        else if (parentQuestion.JobPost != null)
        {
            // Ensure Project is loaded or we fetch it to get HomeownerId
            var homeownerId = parentQuestion.JobPost.Project?.HomeownerId;
            
            if (!homeownerId.HasValue)
            {
                var jobPostFull = await _unitOfWork.JobPosts.GetByIdAsync(parentQuestion.JobPostId);
                homeownerId = jobPostFull?.Project?.HomeownerId;
            }

            if (homeownerId.HasValue && homeownerId.Value != userId)
            {
                await _notificationService.SendNotificationAsync(
                    homeownerId.Value,
                    "New Reply",
                    $"New activity on your job '{parentQuestion.JobPost.Title}'.",
                    parentQuestion.JobPostId,
                    "AuctionQuestion"
                );
            }
        }

        return reply;
    }

    public async Task<JobPostQuestion> EditJobQuestionAsync(Guid questionId, Guid userId, string newText)
    {
        var question = await _unitOfWork.JobPostQuestions.GetByIdAsync(questionId)
            ?? throw new ArgumentException("Question not found");

        // Check if the user is the author (directly or via TradesmanProfile)
        bool isAuthor = question.AuthorId == userId;
        
        if (!isAuthor && question.TradesmanProfileId.HasValue)
        {
            var tradesman = await _unitOfWork.TradesmanProfiles.GetByIdAsync(question.TradesmanProfileId.Value);
            if (tradesman != null && tradesman.UserId == userId)
            {
                isAuthor = true;
            }
        }

        if (!isAuthor)
        {
            throw new UnauthorizedAccessException("You can only edit your own questions.");
        }

        question.UpdateQuestionText(newText);
        
        _unitOfWork.JobPostQuestions.Update(question);
        await _unitOfWork.SaveChangesAsync();

        return question;
    }

    public async Task<JobPostFeedback> EditJobFeedbackAsync(Guid feedbackId, Guid userId, string newText)
    {
        var feedback = await _unitOfWork.JobPostFeedbacks.GetByIdAsync(feedbackId)
            ?? throw new ArgumentException("Feedback not found");

        if (feedback.AuthorId != userId)
        {
            throw new UnauthorizedAccessException("You can only edit your own feedback.");
        }

        feedback.Text = newText;
        feedback.IsEdited = true;
        feedback.UpdatedAt = DateTime.UtcNow;
        
        _unitOfWork.JobPostFeedbacks.Update(feedback);
        await _unitOfWork.SaveChangesAsync();

        return feedback;
    }

    public async Task<JobPostQuestion> EditJobAnswerAsync(Guid questionId, Guid homeownerProfileId, string newAnswer)
    {
        var question = await _unitOfWork.JobPostQuestions.GetByIdAsync(questionId)
            ?? throw new ArgumentException("Question not found");

        if (question.JobPost?.HomeownerProfileId != homeownerProfileId)
        {
            throw new UnauthorizedAccessException("You can only edit answers for your own projects.");
        }

        question.UpdateAnswerText(newAnswer);
        
        _unitOfWork.JobPostQuestions.Update(question);
        await _unitOfWork.SaveChangesAsync();

        return question;
    }

    public async Task<IEnumerable<JobPostQuestion>> GetQuestionRepliesAsync(Guid parentQuestionId, int offset, int limit)
    {
        return await _unitOfWork.JobPostQuestions.GetQueryable()
            .Where(q => q.ParentQuestionId == parentQuestionId)
            .OrderBy(q => q.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .Include(q => q.Author)
            .Include(q => q.TradesmanProfile)
                .ThenInclude(tp => tp!.User)
            .ToListAsync();
    }

    public async Task<int> GetQuestionReplyCountAsync(Guid parentQuestionId)
    {
        return await _unitOfWork.JobPostQuestions.GetQueryable()
            .CountAsync(q => q.ParentQuestionId == parentQuestionId);
    }
}
