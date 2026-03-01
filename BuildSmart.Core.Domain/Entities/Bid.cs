using BuildSmart.Core.Domain.Common;
using BuildSmart.Core.Domain.ValueObjects;

namespace BuildSmart.Core.Domain.Entities;

public class Bid : BaseEntity
{
	public Guid JobPostId { get; set; }
	public JobPost JobPost { get; set; } = null!;

	public Guid TradesmanProfileId { get; set; }
	public TradesmanProfile TradesmanProfile { get; set; } = null!;

	public Amount Amount { get; set; }

	public string? Comment { get; set; }

	/// <summary>
	/// The version of the JobPost this bid was placed against.
	/// If JobPost.AmendmentCount > LinkedAmendmentVersion, this bid is outdated.
	/// </summary>
	public int LinkedAmendmentVersion { get; set; }

	public bool IsAccepted { get; private set; } = false;
	public DateTime? AcceptedAt { get; private set; }

	public bool IsRejected { get; private set; } = false;

	// Computed property for GraphQL/Logic when JobPost is loaded
	public bool IsOutdated => JobPost != null && LinkedAmendmentVersion < JobPost.AmendmentCount;

	// Computed method to check validity against current job state manually
	public bool IsOutdatedVersion(int currentJobVersion) => LinkedAmendmentVersion < currentJobVersion;

	public void Accept()
	{
		IsAccepted = true;
		AcceptedAt = DateTime.UtcNow;
		UpdatedAt = DateTime.UtcNow;
	}

	public void Reject()
	{
		IsRejected = true;
		UpdatedAt = DateTime.UtcNow;
	}
}