using Xunit;
using Moq;
using BuildSmart.SharedUI.ViewModels;
using BuildSmart.SharedUI.GraphQL;
using FluentAssertions;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using StrawberryShake;
using static BuildSmart.SharedUI.ViewModels.JobWizardViewModel;

namespace BuildSmart.Maui.Tests;

public class JobWizardViewModelTests
{
    private readonly Mock<IBuildSmartApiClient> _apiClientMock;
    private readonly JobWizardViewModel _viewModel;

    public JobWizardViewModelTests()
    {
        _apiClientMock = new Mock<IBuildSmartApiClient>();
        
        // Mock GraphQL calls needed during initialization
        var getCategoriesQuery = new Mock<IGetServiceCategoriesQuery>();
        
        var responseMock = new Mock<IOperationResult<IGetServiceCategoriesResult>>();
        responseMock.Setup(r => r.Errors).Returns(new List<IClientError>());
        
        getCategoriesQuery.Setup(q => q.ExecuteAsync(default)).ReturnsAsync(responseMock.Object);
        _apiClientMock.Setup(a => a.GetServiceCategories).Returns(getCategoriesQuery.Object);
        
        _viewModel = new JobWizardViewModel(_apiClientMock.Object);
    }

    [Fact]
    public void ProgressPercentage_InfoStep_Returns15()
    {
        // Arrange (Initial state is Info, Step 0)
        _viewModel.CurrentStep = 0;
        
        // Act
        var progress = _viewModel.ProgressPercentage;

        // Assert
        progress.Should().Be(15);
    }

    [Fact]
    public void ProgressPercentage_CategoryStep_Returns30()
    {
        // Arrange
        _viewModel.CurrentStep = 1;
        
        // Act
        var progress = _viewModel.ProgressPercentage;

        // Assert
        progress.Should().Be(30);
    }

    [Fact]
    public void ProgressPercentage_ReviewStep_Returns100()
    {
        // Arrange
        _viewModel.CurrentStep = 2; // Default InitializeSteps has Info, Category, Review
        
        // Act
        var progress = _viewModel.ProgressPercentage;

        // Assert
        progress.Should().Be(100);
    }

    [Fact]
    public void ProgressPercentage_WithDynamicQuestions_CalculatesCorrectly()
    {
        // Arrange
        var steps = new List<WizardStep>
        {
            new WizardStep { Type = WizardStepType.Info },
            new WizardStep { Type = WizardStepType.CategorySelection },
            new WizardStep { Type = WizardStepType.Questions }, // Question 1
            new WizardStep { Type = WizardStepType.Questions }, // Question 2
            new WizardStep { Type = WizardStepType.Review }
        };
        
        SetWizardSteps(_viewModel, steps);

        // Act & Assert
        
        // Step 2 (First Question)
        _viewModel.CurrentStep = 2;
        // Calculation: 3 steps total between category & review. 
        // 1st q = fraction 1/3. 30 + 70*(1/3) = 53.333...
        _viewModel.ProgressPercentage.Should().BeApproximately(53.33, 0.1);
        
        // Step 3 (Second Question)
        _viewModel.CurrentStep = 3;
        // 2nd q = fraction 2/3. 30 + 70*(2/3) = 76.666...
        _viewModel.ProgressPercentage.Should().BeApproximately(76.66, 0.1);
        
        // Step 4 (Review)
        _viewModel.CurrentStep = 4;
        _viewModel.ProgressPercentage.Should().Be(100);
    }
    
    [Fact]
    public void ProgressPercentage_NoSteps_ReturnsZero()
    {
        // Arrange
        SetWizardSteps(_viewModel, new List<WizardStep>());
        _viewModel.CurrentStep = 0;
        
        // Act
        var progress = _viewModel.ProgressPercentage;

        // Assert
        progress.Should().Be(0);
    }

    private void SetWizardSteps(JobWizardViewModel vm, List<WizardStep> steps)
    {
        var field = typeof(JobWizardViewModel).GetField("_wizardSteps", BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(vm, steps);
    }
}