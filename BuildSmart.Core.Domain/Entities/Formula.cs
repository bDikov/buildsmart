using BuildSmart.Core.Domain.Common;
using System;
using System.Collections.Generic;

namespace BuildSmart.Core.Domain.Entities;

public class Formula : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    // The expression used by the pricing engine, e.g., "global_total_sqm * 3.5"
    public string Expression { get; set; } = string.Empty;

    // Many‑to‑many relationship with questions (spider‑net)
    public ICollection<Question> Questions { get; set; } = new List<Question>();
}
