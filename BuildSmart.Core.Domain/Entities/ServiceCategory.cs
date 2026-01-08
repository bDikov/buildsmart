using BuildSmart.Core.Domain.Common;
using BuildSmart.Core.Domain.Enums;

namespace BuildSmart.Core.Domain.Entities;

public class ServiceCategory : BaseEntity
{
	public string Name { get; set; } = null!;
	public string? Description { get; set; }
    public CategoryStatus Status { get; set; } = CategoryStatus.Draft;

    /// <summary>
    /// Stores the JSON structure for the "Smart Blueprint" questionnaire.
    /// Example: { "questions": [ { "id": "q1", "text": "How many rooms?", "type": "number" } ] }
    /// </summary>
    public string TemplateStructure { get; set; } = "{}";
}