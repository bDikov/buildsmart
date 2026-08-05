using BuildSmart.Core.Domain.Common;
using BuildSmart.Core.Domain.Enums;
using TaskStatus = BuildSmart.Core.Domain.Enums.TaskStatus;

namespace BuildSmart.Core.Domain.Entities;

public class JobTask : BaseEntity
{
    public Guid JobPostId { get; set; }
    public JobPost JobPost { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SequenceOrder { get; set; }

    public decimal EstimatedPrice { get; set; }
    public decimal TradesmanPrice { get; set; }

    public TaskStatus Status { get; set; } = TaskStatus.ToDo;

    public TaskPaymentRecord? PaymentRecord { get; set; }

    public ICollection<TaskSkuItem> SkuItems { get; set; } = new List<TaskSkuItem>();
    public ICollection<TaskAcceptanceCriteria> AcceptanceCriteria { get; set; } = new List<TaskAcceptanceCriteria>();
    public ICollection<BidItem> BidItems { get; set; } = new List<BidItem>();
    public ICollection<JobPostQuestion> Questions { get; set; } = new List<JobPostQuestion>();
    public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();

    public void StartWork()
    {
        Status = TaskStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SubmitForApproval()
    {
        Status = TaskStatus.AwaitingApproval;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Approve()
    {
        Status = TaskStatus.Done;
        UpdatedAt = DateTime.UtcNow;

        if (PaymentRecord == null)
        {
            PaymentRecord = new TaskPaymentRecord
            {
                Id = Guid.NewGuid(),
                JobTaskId = this.Id,
                Status = PaymentStatus.AwaitingPayment,
                CalculatedAmount = EstimatedPrice > 0 ? EstimatedPrice : 0,
                FinalAmount = EstimatedPrice > 0 ? EstimatedPrice : 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }

    public TaskComment? Reject(Guid authorId, string reason)
    {
        if (Status == TaskStatus.AwaitingApproval)
        {
            Status = TaskStatus.InProgress;
            UpdatedAt = DateTime.UtcNow;

            var comment = new TaskComment
            {
                Id = Guid.NewGuid(),
                JobTaskId = this.Id,
                AuthorId = authorId,
                Content = $"[Task Rejection Note]: {reason}",
                IsSystemNote = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            Comments.Add(comment);
            return comment;
        }

        return null;
    }

    public void UpdateDetails(string title, string description, int sequenceOrder)
    {
        Title = title;
        Description = description;
        SequenceOrder = sequenceOrder;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateCriteria(IEnumerable<(Guid? Id, string Description)> newCriteria)
    {
        var inputIds = newCriteria.Where(c => c.Id.HasValue).Select(c => c.Id!.Value).ToHashSet();
        
        var criteriaToDelete = AcceptanceCriteria.Where(c => !inputIds.Contains(c.Id)).ToList();
        foreach (var c in criteriaToDelete)
        {
            AcceptanceCriteria.Remove(c);
        }

        foreach (var input in newCriteria)
        {
            var existing = input.Id.HasValue ? AcceptanceCriteria.FirstOrDefault(c => c.Id == input.Id.Value) : null;
            if (existing != null)
            {
                existing.Description = input.Description;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                AcceptanceCriteria.Add(new TaskAcceptanceCriteria
                {
                    Id = Guid.NewGuid(),
                    JobTaskId = this.Id,
                    Description = input.Description,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }
        UpdatedAt = DateTime.UtcNow;
    }
}