using BuildSmart.Core.Application.Services;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using FluentAssertions;

namespace BuildSmart.Api.Tests;

public class ServiceCategoryOrchestratorTests
{
    [Fact]
    public void OrderCategories_ShouldOrderCorrectly_ByTypeThenByName()
    {
        // Arrange
        var orchestrator = new ServiceCategoryOrchestrator();
        
        var categorySpecific1 = new ServiceCategory { Name = "Plumbing", Type = CategoryType.CategorySpecific };
        var categorySpecific2 = new ServiceCategory { Name = "Electrical", Type = CategoryType.CategorySpecific };
        var globalCategory = new ServiceCategory { Name = "Global Questions", Type = CategoryType.Global };
        var projectDetailsCategory = new ServiceCategory { Name = "Project Details", Type = CategoryType.ProjectDetails };
        var userCategory = new ServiceCategory { Name = "User Info", Type = CategoryType.UserType };

        var categories = new List<ServiceCategory>
        {
            categorySpecific1,
            projectDetailsCategory,
            globalCategory,
            categorySpecific2,
            userCategory
        }.AsQueryable();

        // Act
        var result = orchestrator.OrderCategories(categories).ToList();

        // Assert
        result.Should().HaveCount(5);
        result[0].Should().Be(userCategory); // Type = UserType (0)
        result[1].Should().Be(globalCategory); // Type = Global (1)
        result[2].Should().Be(categorySpecific2); // Type = CategorySpecific (2), Name "Electrical" (alphabetical)
        result[3].Should().Be(categorySpecific1); // Type = CategorySpecific (2), Name "Plumbing"
        result[4].Should().Be(projectDetailsCategory); // Type = ProjectDetails (3)
    }

    [Fact]
    public void OrderCategories_WithNullInput_ShouldReturnEmpty()
    {
        // Arrange
        var orchestrator = new ServiceCategoryOrchestrator();

        // Act
        var result = orchestrator.OrderCategories(null!).ToList();

        // Assert
        result.Should().BeEmpty();
    }
}
