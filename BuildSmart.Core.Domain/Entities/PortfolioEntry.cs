using BuildSmart.Core.Domain.Common;

namespace BuildSmart.Core.Domain.Entities;

public class PortfolioEntry : BaseEntity
{
	public string Title { get; set; }
	public string? Description { get; set; }
	public string ImageUrl { get; set; }
	public string? VideoUrl { get; set; } // As requested, for future video integration

	// --- Relationships ---

	// Foreign key to the TradesmanProfile
	public Guid TradesmanProfileId { get; set; }

	// Navigation property back to the TradesmanProfile
	public TradesmanProfile TradesmanProfile { get; set; } = null!;
}