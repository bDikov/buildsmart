using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BuildSmart.Core.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public AuthService(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    public async Task<User> RegisterUserAsync(string firstName, string lastName, string email, string password)
    {
        // 1. Check if user exists
        var existingUser = await _unitOfWork.Users.GetByEmailAsync(email);
        if (existingUser != null)
        {
            throw new Exception("User with this email already exists.");
        }

        // 2. Hash password
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

        // 3. Generate verification token
        var verificationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));

        // 4. Create user
        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            HashedPassword = hashedPassword,
            Role = UserRoleTypes.Homeowner, // Default role
            IsEmailVerified = false,
            EmailVerificationToken = verificationToken,
            EmailVerificationTokenExpires = DateTime.UtcNow.AddDays(1)
        };

        // Create default Homeowner profile
        user.HomeownerProfile = new HomeownerProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id
        };

        // 5. Add user to repository
        await _unitOfWork.Users.AddAsync(user);
        // await _unitOfWork.HomeownerProfiles.AddAsync(user.HomeownerProfile); // EF Core cascades this usually, but explicit is safer if not configured
        await _unitOfWork.SaveChangesAsync();

        // 6. Send verification email (simulation)
        Console.WriteLine($"Verification email sent to {email}. Token: {verificationToken}");
        Console.WriteLine($"Verification URL: https://localhost:44378/api/auth/verify-email?token={verificationToken}");


        return user;
    }

    public async Task<User> UpdateUserProfileAsync(Guid userId, string firstName, string lastName, string? bio, string? location, string? profilePictureUrl)
    {
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

    public async Task<bool> VerifyEmailAsync(string token)
    {
        var user = await _unitOfWork.Users.GetByVerificationTokenAsync(token);

        if (user == null || user.EmailVerificationTokenExpires < DateTime.UtcNow)
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

    public async Task<string> GenerateJwtTokenForExternalLogin(string email, string name)
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
}
