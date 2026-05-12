using BuildSmart.Core.Domain.Common;

namespace BuildSmart.Core.Domain.Entities;

public class JobTask : BaseEntity
{
    public Guid JobPostId { get; set; }
    public JobPost JobPost { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SequenceOrder { get; set; }

    public decimal EstimatedPrice { get; set; }

    public ICollection<TaskSkuItem> SkuItems { get; set; } = new List<TaskSkuItem>();
    public ICollection<TaskAcceptanceCriteria> AcceptanceCriteria { get; set; } = new List<TaskAcceptanceCriteria>();

    public ICollection<BidItem> BidItems { get; set; } = new List<BidItem>();

    public ICollection<JobPostQuestion> Questions { get; set; } = new List<JobPostQuestion>();

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