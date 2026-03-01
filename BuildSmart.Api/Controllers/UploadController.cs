using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildSmart.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UploadController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMultimediaStorageService _storageService;

    public UploadController(IUnitOfWork unitOfWork, IMultimediaStorageService storageService)
    {
        _unitOfWork = unitOfWork;
        _storageService = storageService;
    }

    [HttpPost("portfolio")]
    [Authorize(Roles = "Tradesman")]
    public async Task<IActionResult> UploadPortfolioEntry([FromForm] string title, [FromForm] string? description, IFormFile file)
    {
        var userId = GetUserId();
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user?.TradesmanProfile == null) return NotFound("Tradesman profile not found.");

        using var stream = file.OpenReadStream();
        var url = await _storageService.SaveFileAsync(stream, file.FileName, file.ContentType);

        var entry = new PortfolioEntry
        {
            Title = title,
            Description = description,
            ImageUrl = url,
            TradesmanProfileId = user.TradesmanProfile.Id
        };

        user.TradesmanProfile.PortfolioEntries.Add(entry);
        await _unitOfWork.SaveChangesAsync();

        return Ok(entry);
    }

    [HttpPost("certification")]
    [Authorize(Roles = "Tradesman")]
    public async Task<IActionResult> UploadCertification([FromForm] string title, [FromForm] string? description, [FromForm] DateTime issuedAt, [FromForm] DateTime? expiresAt, IFormFile file)
    {
        var userId = GetUserId();
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user?.TradesmanProfile == null) return NotFound("Tradesman profile not found.");

        using var stream = file.OpenReadStream();
        var url = await _storageService.SaveFileAsync(stream, file.FileName, file.ContentType);

        var cert = new Certification
        {
            Title = title,
            Description = description,
            DocumentUrl = url,
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt,
            TradesmanProfileId = user.TradesmanProfile.Id
        };

        user.TradesmanProfile.Certifications.Add(cert);
        await _unitOfWork.SaveChangesAsync();

        return Ok(cert);
    }

    [HttpPost("video-intro")]
    [Authorize(Roles = "Tradesman")]
    public async Task<IActionResult> UpdateVideoIntroduction(IFormFile file)
    {
        var userId = GetUserId();
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user?.TradesmanProfile == null) return NotFound("Tradesman profile not found.");

        if (!string.IsNullOrEmpty(user.TradesmanProfile.VideoIntroductionUrl))
        {
            await _storageService.DeleteFileAsync(user.TradesmanProfile.VideoIntroductionUrl);
        }

        using var stream = file.OpenReadStream();
        var url = await _storageService.SaveFileAsync(stream, file.FileName, file.ContentType);

        user.TradesmanProfile.VideoIntroductionUrl = url;
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { VideoUrl = url });
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !Guid.TryParse(claim.Value, out var userId))
        {
            throw new UnauthorizedAccessException();
        }
        return userId;
    }
}
