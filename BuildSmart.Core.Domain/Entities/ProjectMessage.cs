using BuildSmart.Core.Domain.Common;
using System;

namespace BuildSmart.Core.Domain.Entities;

public class ProjectMessage : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public Guid SenderId { get; set; }
    public User Sender { get; set; } = null!;

    public string MessageText { get; set; } = null!;
}
