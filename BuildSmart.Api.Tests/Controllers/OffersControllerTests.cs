using System;
using System.Threading.Tasks;
using BuildSmart.Api.Controllers;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Application.Resources;
using BuildSmart.Core.Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace BuildSmart.Api.Tests.Controllers;

public class OffersControllerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IProjectRepository> _projectRepoMock;
    private readonly Mock<IAiCalculationRepository> _aiCalcRepoMock;
    private readonly Mock<IPdfGeneratorService> _pdfGeneratorServiceMock;
    private readonly Mock<IStringLocalizer<OfferResources>> _localizerMock;
    private readonly Mock<IAiService> _aiServiceMock;
    private readonly OffersController _controller;

    public OffersControllerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _projectRepoMock = new Mock<IProjectRepository>();
        _aiCalcRepoMock = new Mock<IAiCalculationRepository>();
        _pdfGeneratorServiceMock = new Mock<IPdfGeneratorService>();
        _localizerMock = new Mock<IStringLocalizer<OfferResources>>();
        _aiServiceMock = new Mock<IAiService>();

        _unitOfWorkMock.Setup(u => u.Projects).Returns(_projectRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.AiCalculations).Returns(_aiCalcRepoMock.Object);

        var mockLocalized = new LocalizedString("Label_Subtotal", "Subtotal for {0}");
        _localizerMock.Setup(l => l["Label_Subtotal"]).Returns(mockLocalized);

        _controller = new OffersController(
            _unitOfWorkMock.Object,
            _pdfGeneratorServiceMock.Object,
            _localizerMock.Object,
            _aiServiceMock.Object);
    }

    [Fact]
    public async Task DownloadOfferPdf_ShouldReturnNotFound_WhenProjectDoesNotExist()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        _projectRepoMock.Setup(r => r.GetByIdAsync(projectId)).ReturnsAsync((Project?)null);

        // Act
        var result = await _controller.DownloadOfferPdf(projectId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>()
            .Which.Value.Should().Be("Project not found.");
    }

    [Fact]
    public async Task DownloadOfferPdf_ShouldReturnBadRequest_WhenProjectHasNoPdfAndNoCalculations()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var project = new Project
        {
            Id = projectId,
            Title = "Test Project",
            MasterOfferPdf = null
        };

        _projectRepoMock.Setup(r => r.GetByIdAsync(projectId)).ReturnsAsync(project);
        _aiCalcRepoMock.Setup(r => r.GetByProjectWithTasksAsync(projectId)).ReturnsAsync(new List<AiCalculation>());

        // Act
        var result = await _controller.DownloadOfferPdf(projectId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().Be("No categories have been priced yet for this project. Please run calculations first.");
    }

    [Fact]
    public async Task DownloadOfferPdf_ShouldReturnFileContentResult_WhenPdfExists()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var pdfBytes = new byte[] { 1, 2, 3, 4 };
        var projectTitle = "Kitchen Remodel";
        
        var project = new Project
        {
            Id = projectId,
            Title = projectTitle,
            MasterOfferPdf = pdfBytes
        };

        _projectRepoMock.Setup(r => r.GetByIdAsync(projectId)).ReturnsAsync(project);

        // Act
        var result = await _controller.DownloadOfferPdf(projectId);

        // Assert
        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.ContentType.Should().Be("application/pdf");
        fileResult.FileDownloadName.Should().Be($"{projectTitle}_Offer.pdf");
        fileResult.FileContents.Should().BeEquivalentTo(pdfBytes);
    }
}
