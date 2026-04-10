using BuildSmart.Core.Domain.Common;

namespace BuildSmart.Core.Domain.Entities;

public class TaskAcceptanceCriteria : BaseEntity
{
    public Guid JobTaskId { get; set; }
    public JobTask JobTask { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
}