using Microsoft.AspNetCore.Mvc;
using BuildSmart.Core.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;

namespace BuildSmart.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OffersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public OffersController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("{projectId}/download")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadOfferPdf(Guid projectId)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
        
        if (project == null || project.MasterOfferPdf == null || project.MasterOfferPdf.Length == 0)
        {
            return NotFound("Offer PDF not found for this project.");
        }

        return File(project.MasterOfferPdf, "application/pdf", $"{project.Title}_Offer.pdf");
    }
}
