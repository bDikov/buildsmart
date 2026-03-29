using BuildSmart.Core.Domain.Common;

namespace BuildSmart.Core.Domain.Entities;

public class JobTask : BaseEntity
{
    public Guid JobPostId { get; set; }
    public JobPost JobPost { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SequenceOrder { get; set; }

    public ICollection<TaskAcceptanceCriteria> AcceptanceCriteria { get; set; } = new List<TaskAcceptanceCriteria>();
    
    public ICollection<BidItem> BidItems { get; set; } = new List<BidItem>();

    public ICollection<JobPostQuestion> Questions { get; set; } = new List<JobPostQuestion>();
}