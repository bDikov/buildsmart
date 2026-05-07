using System;
using System.Threading.Tasks;
using BuildSmart.Api.Controllers;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BuildSmart.Api.Tests.Controllers;

public class OffersControllerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IProjectRepository> _projectRepoMock;
    private readonly OffersController _controller;

    public OffersControllerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _projectRepoMock = new Mock<IProjectRepository>();

        _unitOfWorkMock.Setup(u => u.Projects).Returns(_projectRepoMock.Object);

        _controller = new OffersController(_unitOfWorkMock.Object);
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
            .Which.Value.Should().Be("Offer PDF not found for this project.");
    }

    [Fact]
    public async Task DownloadOfferPdf_ShouldReturnNotFound_WhenProjectHasNoPdf()
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

        // Act
        var result = await _controller.DownloadOfferPdf(projectId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>()
            .Which.Value.Should().Be("Offer PDF not found for this project.");
    }

    [Fact]
    public async Task DownloadOfferPdf_ShouldReturnNotFound_WhenPdfIsEmpty()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var project = new Project
        {
            Id = projectId,
            Title = "Test Project",
            MasterOfferPdf = Array.Empty<byte>()
        };

        _projectRepoMock.Setup(r => r.GetByIdAsync(projectId)).ReturnsAsync(project);

        // Act
        var result = await _controller.DownloadOfferPdf(projectId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>()
            .Which.Value.Should().Be("Offer PDF not found for this project.");
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
