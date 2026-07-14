using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BuildSmart.Core.Application.Services;

public class AuthService : IAuthService
{
	private readonly IUnitOfWork _unitOfWork;
	private readonly IConfiguration _configuration;
	private readonly IEmailService _emailService;

	public AuthService(IUnitOfWork unitOfWork, IConfiguration configuration, IEmailService emailService)
	{
		_unitOfWork = unitOfWork;
		_configuration = configuration;
		_emailService = emailService;
	}

	private bool IsValidBulgarianPhoneNumber(string? phoneNumber)
	{
		if (string.IsNullOrWhiteSpace(phoneNumber))
			return false;

		// Normalize by removing spaces, dashes, and parentheses
		var normalized = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

		// Check Bulgarian format using regex (supporting landlines and mobile, starting with +359, 00359, or 0)
		var regex = new System.Text.RegularExpressions.Regex(@"^(?:\+359|00359|0)([2-9]\d{7,8}|8[7-9]\d{7}|9[8-9]\d{7})$");
		return regex.IsMatch(normalized);
	}

	public async Task<User> RegisterUserAsync(string firstName, string lastName, string email, string password, string? phoneNumber = null)
	{
		// 1. Validate inputs
		if (string.IsNullOrWhiteSpace(password))
		{
			throw new Exception("Password is required for standard registration.");
		}

		if (!string.IsNullOrWhiteSpace(phoneNumber) && !IsValidBulgarianPhoneNumber(phoneNumber))
		{
			throw new Exception("Please enter a valid Bulgarian phone number.");
		}

		// 2. Check if user exists
		var existingUser = await _unitOfWork.Users.GetByEmailAsync(email);
		if (existingUser != null)
		{
			throw new Exception("User with this email already exists.");
		}

		// 3. Hash password
		var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

		// 4. Generate 6-digit verification code
		var verificationToken = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");

		// 5. Create user
		var user = new User
		{
			FirstName = firstName,
			LastName = lastName,
			Email = email,
			PhoneNumber = phoneNumber,
			HashedPassword = hashedPassword,
			Role = UserRoleTypes.Homeowner, // Default role
			IsEmailVerified = false,
			EmailVerificationToken = verificationToken,
			EmailVerificationTokenExpires = DateTime.UtcNow.AddMinutes(30) // Expire in 30 minutes
		};

		// Create default Homeowner profile
		user.HomeownerProfile = new HomeownerProfile
		{
			Id = Guid.NewGuid(),
			UserId = user.Id
		};

		// 5. Add user to repository
		await _unitOfWork.Users.AddAsync(user);
		await _unitOfWork.SaveChangesAsync();

		// 6. Send verification email
		var emailSubject = "Confirm your BuildSmart Account";
		var emailBody = $@"
			<html>
			<body style='font-family: Arial, sans-serif; line-height: 1.6;'>
				<h2>Welcome to BuildSmart!</h2>
				<p>Thank you for registering. Please use the following 6-digit verification code to confirm your email address and activate your account:</p>
				<div style='font-size: 24px; font-weight: bold; letter-spacing: 5px; margin: 20px 0; color: #1E3A8A;'>
					{verificationToken}
				</div>
				<p>This code will expire in 30 minutes.</p>
				<p>If you did not create this account, please ignore this email.</p>
				<br/>
				<p>Best regards,<br/>The BuildSmart Team</p>
			</body>
			</html>";

		try
		{
			await _emailService.SendGenericEmailAsync(email, emailSubject, emailBody);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Failed to send verification email to {email}: {ex.Message}");
		}

		// Console Logging for local debug convenience
		Console.WriteLine($"Verification email sent to {email}. Code: {verificationToken}");

		return user;
	}

	public async Task<User> UpdateUserProfileAsync(Guid userId, string firstName, string lastName, string? bio, string? location, string? profilePictureUrl, string? phoneNumber, string? email)
	{
		if (!string.IsNullOrWhiteSpace(phoneNumber) && !IsValidBulgarianPhoneNumber(phoneNumber))
		{
			throw new Exception("Please enter a valid Bulgarian phone number.");
		}

		var user = await _unitOfWork.Users.GetByIdAsync(userId);
		if (user == null)
		{
			throw new Exception("User not found.");
		}

		user.FirstName = firstName;
		user.LastName = lastName;
		user.Bio = bio;
		user.Location = location;
		user.ProfilePictureUrl = profilePictureUrl;
		user.PhoneNumber = phoneNumber;

		// Note: email/username is read-only during profile updates to protect credentials integrity.

		user.UpdatedAt = DateTime.UtcNow;

		_unitOfWork.Users.Update(user);
		await _unitOfWork.SaveChangesAsync();

		return user;
	}

	public async Task<User> UpdateUserRoleAndCategoriesAsync(Guid userId, UserRoleTypes newRole, List<Guid>? serviceCategoryIds)
	{
		var user = await _unitOfWork.Users.GetByIdAsync(userId);
		if (user == null)
		{
			throw new Exception("User not found.");
		}

		// 1. Update Role
		user.Role = newRole;
		user.UpdatedAt = DateTime.UtcNow;

		// 2. Handle Tradesman Profile logic
		if (newRole == UserRoleTypes.Tradesman)
		{
			// Ensure TradesmanProfile exists
			if (user.TradesmanProfile == null)
			{
				user.TradesmanProfile = new TradesmanProfile
				{
					Id = Guid.NewGuid(),
					UserId = user.Id,
					CreatedAt = DateTime.UtcNow,
					UpdatedAt = DateTime.UtcNow
				};
			}

			// Update Categories if provided
			if (serviceCategoryIds != null)
			{
				// Clear existing skills (or sync them)
				user.TradesmanProfile.Skills.Clear();
				foreach (var catId in serviceCategoryIds)
				{
					user.TradesmanProfile.Skills.Add(new BuildSmart.Core.Domain.Entities.JoinEntities.TradesmanSkill
					{
						ServiceCategoryId = catId,
						VerificationStatus = SkillVerificationStatus.PortfolioVerified, // Admin promotion
						CreatedAt = DateTime.UtcNow,
						UpdatedAt = DateTime.UtcNow
					});
				}
			}
		}

		_unitOfWork.Users.Update(user);
		await _unitOfWork.SaveChangesAsync();

		return user;
	}

	public async Task<bool> VerifyEmailAsync(string email, string code)
	{
		var user = await _unitOfWork.Users.GetByEmailAsync(email);

		if (user == null || user.EmailVerificationToken != code || user.EmailVerificationTokenExpires < DateTime.UtcNow)
		{
			return false;
		}

		user.IsEmailVerified = true;
		user.EmailVerificationToken = null;
		user.EmailVerificationTokenExpires = null;

		_unitOfWork.Users.Update(user);
		await _unitOfWork.SaveChangesAsync();

		return true;
	}

	public async Task<bool> ResendVerificationCodeAsync(string email)
	{
		var user = await _unitOfWork.Users.GetByEmailAsync(email);
		if (user == null)
		{
			throw new Exception("User not found.");
		}

		if (user.IsEmailVerified)
		{
			throw new Exception("Email is already verified.");
		}

		// Generate new 6-digit verification code
		var verificationCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");

		user.EmailVerificationToken = verificationCode;
		user.EmailVerificationTokenExpires = DateTime.UtcNow.AddMinutes(30);

		_unitOfWork.Users.Update(user);
		await _unitOfWork.SaveChangesAsync();

		// Send email
		var emailSubject = "Confirm your BuildSmart Account";
		var emailBody = $@"
			<html>
			<body style='font-family: Arial, sans-serif; line-height: 1.6;'>
				<h2>Welcome to BuildSmart!</h2>
				<p>Please use the following 6-digit verification code to confirm your email address and activate your account:</p>
				<div style='font-size: 24px; font-weight: bold; letter-spacing: 5px; margin: 20px 0; color: #1E3A8A;'>
					{verificationCode}
				</div>
				<p>This code will expire in 30 minutes.</p>
				<br/>
				<p>Best regards,<br/>The BuildSmart Team</p>
			</body>
			</html>";

		try
		{
			await _emailService.SendGenericEmailAsync(email, emailSubject, emailBody);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Failed to send verification email to {email}: {ex.Message}");
		}

		// Console Logging for local debug convenience
		Console.WriteLine($"Verification email resent to {email}. Code: {verificationCode}");

		return true;
	}

	public async Task<string> GenerateJwtTokenForExternalLogin(string email, string name, string? profilePictureUrl = null)
	{
		var user = await _unitOfWork.Users.GetByEmailAsync(email);

		if (user == null)
		{
			var names = name.Split(' ');
			var firstName = names.Length > 0 ? names[0] : string.Empty;
			var lastName = names.Length > 1 ? names[1] : string.Empty;

			user = new User
			{
				FirstName = firstName,
				LastName = lastName,
				Email = email,
				HashedPassword = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // Satisfy the NOT NULL DB constraint for OAuth users
				ProfilePictureUrl = profilePictureUrl,
				Role = UserRoleTypes.Homeowner, // Default role
				IsEmailVerified = true, // Email is verified by the external provider
			};

			user.HomeownerProfile = new HomeownerProfile
			{
				Id = Guid.NewGuid(),
				UserId = user.Id
			};

			await _unitOfWork.Users.AddAsync(user);
			await _unitOfWork.SaveChangesAsync();
		}
		else if (string.IsNullOrEmpty(user.ProfilePictureUrl) && !string.IsNullOrEmpty(profilePictureUrl))
		{
			// Update the user's profile picture if they don't have one
			user.ProfilePictureUrl = profilePictureUrl;
			_unitOfWork.Users.Update(user);
			await _unitOfWork.SaveChangesAsync();
		}

		var tokenHandler = new JwtSecurityTokenHandler();
		var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not found"));
		var tokenDescriptor = new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity(new[]
			{
				new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
				new Claim(ClaimTypes.Email, user.Email),
				new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
				new Claim(ClaimTypes.Role, user.Role.ToString())
			}),
			Expires = DateTime.UtcNow.AddDays(7),
			Issuer = _configuration["Jwt:Issuer"],
			Audience = _configuration["Jwt:Audience"],
			SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
		};
		var token = tokenHandler.CreateToken(tokenDescriptor);
		return tokenHandler.WriteToken(token);
	}

	public async Task<User> PromoteGuestToUserAsync(Guid guestUserId, string firstName, string lastName, string newEmail, string password, string? phoneNumber = null)
	{
		// 1. Validate inputs
		if (string.IsNullOrWhiteSpace(password))
		{
			throw new Exception("Password is required for registration.");
		}

		if (!string.IsNullOrWhiteSpace(phoneNumber) && !IsValidBulgarianPhoneNumber(phoneNumber))
		{
			throw new Exception("Please enter a valid Bulgarian phone number.");
		}

		// 2. Fetch the guest user
		var guestUser = await _unitOfWork.Users.GetByIdAsync(guestUserId);
		if (guestUser == null)
		{
			throw new Exception("Guest user not found.");
		}

		// Ensure they are actually a guest
		if (guestUser.Email == null || !guestUser.Email.EndsWith("@buildsmart.guest", StringComparison.OrdinalIgnoreCase))
		{
			throw new Exception("The user is already a standard user or not a guest.");
		}

		// 3. Check if a standard user with the new email already exists
		var existingUser = await _unitOfWork.Users.GetByEmailAsync(newEmail);
		if (existingUser != null)
		{
			throw new Exception("User with this email already exists.");
		}

		// 4. Update the user properties in place
		guestUser.Email = newEmail;
		guestUser.FirstName = firstName;
		guestUser.LastName = lastName;
		guestUser.PhoneNumber = phoneNumber;
		guestUser.HashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
		guestUser.IsEmailVerified = false;

		// Generate 6-digit verification code
		var verificationToken = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
		guestUser.EmailVerificationToken = verificationToken;
		guestUser.EmailVerificationTokenExpires = DateTime.UtcNow.AddMinutes(30);

		_unitOfWork.Users.Update(guestUser);
		await _unitOfWork.SaveChangesAsync();

		// 5. Send verification email
		var emailSubject = "Confirm your BuildSmart Account";
		var emailBody = $@"
			<html>
			<body style='font-family: Arial, sans-serif; line-height: 1.6;'>
				<h2>Welcome to BuildSmart!</h2>
				<p>Your guest account has been successfully promoted to a registered account. Please use the following 6-digit verification code to confirm your email address and activate your account:</p>
				<div style='font-size: 24px; font-weight: bold; letter-spacing: 5px; margin: 20px 0; color: #1E3A8A;'>
					{verificationToken}
				</div>
				<p>This code will expire in 30 minutes.</p>
				<p>If you did not initiate this change, please ignore this email.</p>
				<br/>
				<p>Best regards,<br/>The BuildSmart Team</p>
			</body>
			</html>";

		try
		{
			await _emailService.SendGenericEmailAsync(newEmail, emailSubject, emailBody);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Failed to send verification email to {newEmail}: {ex.Message}");
		}

		// Console Logging for local debug convenience
		Console.WriteLine($"Verification email sent to {newEmail}. Code: {verificationToken}");

		return guestUser;
	}
}