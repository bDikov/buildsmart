using BuildSmart.Core.Domain.Common;

namespace BuildSmart.Core.Domain.Entities;

public class JobPostFeedback : BaseEntity
{
    public Guid JobPostId { get; set; }
    public JobPost JobPost { get; set; } = null!;

    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public string Text { get; set; } = null!;

    /// <summary>
    /// Admins can mark a specific feedback item as resolved once the user clarifies or fixes it.
    /// </summary>
    public bool IsResolved { get; set; } = false;
    public bool IsEdited { get; set; } = false;

    public Guid? ParentFeedbackId { get; set; }
    public JobPostFeedback? ParentFeedback { get; set; }
    public ICollection<JobPostFeedback> Replies { get; set; } = new List<JobPostFeedback>();
}