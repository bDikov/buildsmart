using BuildSmart.Core.Domain.Common;

namespace BuildSmart.Core.Domain.Entities;

public class LocalizationResource : BaseEntity
{
    public string Key { get; set; } = null!;
    public string Culture { get; set; } = null!;
    public string Value { get; set; } = null!;
    public string? UpdatedBy { get; set; }
}
