using System;
using BuildSmart.Core.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace BuildSmart.Core.Domain.Tests.Entities;

public class ProjectTests
{
    [Fact]
    public void HasOfferPdf_ShouldReturnFalse_WhenPdfIsNull()
    {
        // Arrange
        var project = new Project
        {
            Title = "Test Project",
            Description = "Test Desc"
        };

        // Act
        var result = project.HasOfferPdf;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasOfferPdf_ShouldReturnFalse_WhenPdfIsEmptyArray()
    {
        // Arrange
        var project = new Project
        {
            Title = "Test Project",
            Description = "Test Desc",
            MasterOfferPdf = Array.Empty<byte>()
        };

        // Act
        var result = project.HasOfferPdf;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasOfferPdf_ShouldReturnTrue_WhenPdfHasData()
    {
        // Arrange
        var project = new Project
        {
            Title = "Test Project",
            Description = "Test Desc",
            MasterOfferPdf = new byte[] { 1, 2, 3 }
        };

        // Act
        var result = project.HasOfferPdf;

        // Assert
        result.Should().BeTrue();
    }
}
