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
    private readonly IMediaService _mediaService;

    public UploadController(
        IUnitOfWork unitOfWork,
        IMultimediaStorageService storageService,
        IMediaService mediaService)
    {
        _unitOfWork = unitOfWork;
        _storageService = storageService;
        _mediaService = mediaService;
    }

    [HttpPost("portfolio")]
    [Authorize(Roles = "Tradesman")]
    public async Task<IActionResult> UploadPortfolioEntry([FromForm] string title, [FromForm] string? description, IFormFile file)
    {
        var userId = GetUserId();
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user?.TradesmanProfile == null) return NotFound("Tradesman profile not found.");

        string url;
        try
        {
            using var stream = file.OpenReadStream();
            url = await _mediaService.UploadFileAsync(stream, file.FileName, file.ContentType ?? "image/jpeg");
        }
        catch
        {
            using var stream = file.OpenReadStream();
            url = await _storageService.SaveFileAsync(stream, file.FileName, file.ContentType ?? "image/jpeg");
        }

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

        string url;
        try
        {
            using var stream = file.OpenReadStream();
            url = await _mediaService.UploadFileAsync(stream, file.FileName, file.ContentType ?? "application/pdf");
        }
        catch
        {
            using var stream = file.OpenReadStream();
            url = await _storageService.SaveFileAsync(stream, file.FileName, file.ContentType ?? "application/pdf");
        }

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
            try { await _mediaService.DeleteFileAsync(user.TradesmanProfile.VideoIntroductionUrl); } catch { }
            try { await _storageService.DeleteFileAsync(user.TradesmanProfile.VideoIntroductionUrl); } catch { }
        }

        string url;
        try
        {
            using var stream = file.OpenReadStream();
            url = await _mediaService.UploadFileAsync(stream, file.FileName, file.ContentType ?? "video/mp4");
        }
        catch
        {
            using var stream = file.OpenReadStream();
            url = await _storageService.SaveFileAsync(stream, file.FileName, file.ContentType ?? "video/mp4");
        }

        user.TradesmanProfile.VideoIntroductionUrl = url;
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { VideoUrl = url });
    }

    [HttpPost("admin-image")]
    [Authorize(Roles = "Admin, ADMIN, admin")]
    public async Task<IActionResult> UploadAdminImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        string url;
        try
        {
            using var stream = file.OpenReadStream();
            url = await _mediaService.UploadFileAsync(stream, file.FileName, file.ContentType ?? "image/jpeg");
        }
        catch
        {
            using var stream = file.OpenReadStream();
            url = await _storageService.SaveFileAsync(stream, file.FileName, file.ContentType ?? "image/jpeg");
        }

        return Ok(new { Url = url });
    }

    [HttpPost("landing-media")]
    [Authorize(Roles = "Admin, ADMIN, admin")]
    public async Task<IActionResult> UploadLandingMedia(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        string url;
        try
        {
            using var stream = file.OpenReadStream();
            url = await _mediaService.UploadFileAsync(stream, file.FileName, file.ContentType ?? "image/jpeg");
        }
        catch
        {
            using var stream = file.OpenReadStream();
            url = await _storageService.SaveFileAsync(stream, file.FileName, file.ContentType ?? "image/jpeg");
        }

        return Ok(new { Url = url, Type = file.ContentType != null && file.ContentType.StartsWith("video") ? "video" : "image" });
    }

    [HttpPost("folder-upload")]
    [Authorize(Roles = "Admin, ADMIN, admin, Tradesman, tradesman")]
    public async Task<IActionResult> UploadToFolder(
        IFormFile file,
        [FromForm] Guid? folderId,
        [FromServices] IUnifiedMediaService unifiedMediaService)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var userId = GetUserId();
        using var stream = file.OpenReadStream();
        var asset = await unifiedMediaService.UploadAndOptimizeImageAsync(
            stream,
            file.FileName,
            file.ContentType ?? "application/octet-stream",
            folderId,
            userId);

        return Ok(asset);
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
