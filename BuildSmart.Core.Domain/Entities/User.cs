using BuildSmart.Core.Domain.Common;
using BuildSmart.Core.Domain.Enums;

namespace BuildSmart.Core.Domain.Entities;

public class User : BaseEntity
{
	private UserRoleTypes role;

	public string FirstName { get; set; } = null!;
	public string LastName { get; set; }
	public string Email { get; set; }
	public string HashedPassword { get; set; }
	public string? PhoneNumber { get; set; }

	public UserRoleTypes Role { get => role; set => role = value; }
	public string? Bio { get; set; }
	public string? Location { get; set; }
	public string? ProfilePictureUrl { get; set; }
	public bool IsEmailVerified { get; set; }
	public string? EmailVerificationToken { get; set; }
	public DateTime? EmailVerificationTokenExpires { get; set; }

    public virtual HomeownerProfile? HomeownerProfile { get; set; }
	public virtual TradesmanProfile? TradesmanProfile { get; set; }
}