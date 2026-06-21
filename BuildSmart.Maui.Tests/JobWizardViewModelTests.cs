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
    public void ProgressPercentage_CategoryStep_Returns15()
    {
        // Arrange
        _viewModel.CurrentStep = 0;
        _viewModel.SelectableCategories = new System.Collections.ObjectModel.ObservableCollection<SelectableCategoryViewModel>
        {
            new SelectableCategoryViewModel(new Mock<IGetServiceCategories_ServiceCategories>().Object) { IsSelected = true }
        };
        
        // Act
        var progress = _viewModel.ProgressPercentage;

        // Assert
        progress.Should().Be(15);
    }

    [Fact]
    public void ProgressPercentage_ReviewStep_Returns70()
    {
        // Arrange
        _viewModel.CurrentStep = 1; // Default InitializeSteps has CategorySelection, Review, Info
        
        // Act
        var progress = _viewModel.ProgressPercentage;

        // Assert
        progress.Should().Be(70);
    }

    [Fact]
    public void ProgressPercentage_InfoStep_Returns70To100()
    {
        // Arrange
        _viewModel.CurrentStep = 2; // Info Step
        
        var q1 = new WizardQuestionViewModel { Id = "q1", Type = "text", IsVisible = true, Answer = "" };
        var q2 = new WizardQuestionViewModel { Id = "q2", Type = "text", IsVisible = true, Answer = "" };
        
        _viewModel.Questions.Clear();
        _viewModel.Questions.Add(q1);
        _viewModel.Questions.Add(q2);
        
        // Act & Assert 1: 0 answered -> 70
        _viewModel.ProgressPercentage.Should().Be(70);

        // Act & Assert 2: 1 answered -> 70 + 30 * (1/2) = 85
        q1.Answer = "Some Location";
        _viewModel.ProgressPercentage.Should().Be(85);

        // Act & Assert 3: 2 answered -> 70 + 30 * (2/2) = 100
        q2.Answer = "Referral Info";
        _viewModel.ProgressPercentage.Should().Be(100);
    }

    [Fact]
    public void ProgressPercentage_WithDynamicQuestions_CalculatesCorrectly()
    {
        // Arrange
        var steps = new List<WizardStep>
        {
            new WizardStep { Type = WizardStepType.CategorySelection },
            new WizardStep { Type = WizardStepType.Questions }, // Question 1
            new WizardStep { Type = WizardStepType.Questions }, // Question 2
            new WizardStep { Type = WizardStepType.Review },
            new WizardStep { Type = WizardStepType.Info }
        };
        
        SetWizardSteps(_viewModel, steps);

        // Act & Assert
        
        // Step 1 (First Question)
        _viewModel.CurrentStep = 1;
        // Calculation: 2 steps total between category & review. 
        // 1st q = fraction 1/2. 15 + 55*(1/2) = 42.5
        _viewModel.ProgressPercentage.Should().Be(42.5);
        
        // Step 2 (Second Question)
        _viewModel.CurrentStep = 2;
        // 2nd q = fraction 2/2. 15 + 55*(2/2) = 70.0
        _viewModel.ProgressPercentage.Should().Be(70.0);
        
        // Step 3 (Review)
        _viewModel.CurrentStep = 3;
        _viewModel.ProgressPercentage.Should().Be(70.0);

        // Step 4 (Info)
        _viewModel.CurrentStep = 4;
        _viewModel.ProgressPercentage.Should().Be(70.0);
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

    [Fact]
    public async Task LoadCategoriesAsync_WithZeroProjects_SetsHasProjectsToFalse()
    {
        // Arrange
        var getProjectsQuery = new Mock<IGetMyProjectsQuery>();
        var projectsResponseMock = new Mock<IOperationResult<IGetMyProjectsResult>>();
        projectsResponseMock.Setup(r => r.Errors).Returns(new List<IClientError>());
        
        var resultDataMock = new Mock<IGetMyProjectsResult>();
        resultDataMock.Setup(d => d.MyProjects).Returns(new List<IGetMyProjects_MyProjects>());
        projectsResponseMock.Setup(r => r.Data).Returns(resultDataMock.Object);
        
        getProjectsQuery.Setup(q => q.ExecuteAsync(default)).ReturnsAsync(projectsResponseMock.Object);
        _apiClientMock.Setup(a => a.GetMyProjects).Returns(getProjectsQuery.Object);

        // Act
        await _viewModel.LoadCategoriesAsync();

        // Assert
        _viewModel.HasProjects.Should().BeFalse();
    }

    [Fact]
    public async Task LoadCategoriesAsync_WithExistingProjects_SetsHasProjectsToTrue()
    {
        // Arrange
        var getProjectsQuery = new Mock<IGetMyProjectsQuery>();
        var projectsResponseMock = new Mock<IOperationResult<IGetMyProjectsResult>>();
        projectsResponseMock.Setup(r => r.Errors).Returns(new List<IClientError>());
        
        var resultDataMock = new Mock<IGetMyProjectsResult>();
        
        var projectMock = new Mock<IGetMyProjects_MyProjects>();
        resultDataMock.Setup(d => d.MyProjects).Returns(new List<IGetMyProjects_MyProjects> { projectMock.Object });
        
        projectsResponseMock.Setup(r => r.Data).Returns(resultDataMock.Object);
        
        getProjectsQuery.Setup(q => q.ExecuteAsync(default)).ReturnsAsync(projectsResponseMock.Object);
        _apiClientMock.Setup(a => a.GetMyProjects).Returns(getProjectsQuery.Object);

        // Act
        await _viewModel.LoadCategoriesAsync();

        // Assert
        _viewModel.HasProjects.Should().BeTrue();
    }

    [Fact]
    public void EvaluateQuestionVisibility_DeepNesting_UpdatesVisibilityCorrectly()
    {
        // Arrange
        var q1 = new WizardQuestionViewModel { Id = "q1", Type = "multiselect", Answer = "", IsVisible = true };
        var q2 = new WizardQuestionViewModel { Id = "q2", Type = "multiselect", DependsOn = "q1", DependsOnValue = "OptionA", Answer = "", IsVisible = false };
        var q3 = new WizardQuestionViewModel { Id = "q3", Type = "number", DependsOn = "q2", DependsOnValue = "OptionB", Answer = "", IsVisible = false };
        
        _viewModel.Questions.Add(q1);
        _viewModel.Questions.Add(q2);
        _viewModel.Questions.Add(q3);

        // Attach property changed manually to mimic the internal LoadStepData behavior
        var method = typeof(JobWizardViewModel).GetMethod("EvaluateQuestionVisibility", BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act 1: Answer Q1 to show Q2
        q1.Answer = "OptionA";
        method.Invoke(_viewModel, null);
        
        // Assert 1
        q2.IsVisible.Should().BeTrue();
        q3.IsVisible.Should().BeFalse();

        // Act 2: Answer Q2 to show Q3
        q2.Answer = "OptionB";
        method.Invoke(_viewModel, null);
        
        // Assert 2
        q3.IsVisible.Should().BeTrue();

        // Act 3: Remove answer from Q1. Both Q2 and Q3 should hide.
        q1.Answer = "";
        method.Invoke(_viewModel, null);
        
        // Assert 3
        q2.IsVisible.Should().BeFalse();
        q3.IsVisible.Should().BeFalse(); // Deep nesting resolves correctly
    }

    [Fact]
    public void GetLocalizedValue_ReturnsCorrectLanguageString()
    {
        // Arrange
        var method = typeof(JobWizardViewModel).GetMethod("GetLocalizedValue", BindingFlags.NonPublic | BindingFlags.Instance);
        
        var jsonString = "{ \"bg\": \"Здравей\", \"en\": \"Hello\" }";
        var node = System.Text.Json.Nodes.JsonNode.Parse(jsonString);

        // Act & Assert
        var resultBg = method.Invoke(_viewModel, new object[] { node, "bg", "en" });
        resultBg.Should().Be("Здравей");

        var resultEn = method.Invoke(_viewModel, new object[] { node, "en", "bg" });
        resultEn.Should().Be("Hello");

        var resultFallback = method.Invoke(_viewModel, new object[] { node, "fr", "en" });
        resultFallback.Should().Be("Hello");
    }

    [Fact]
    public void GetLocalizedValue_WithPlainString_ReturnsString()
    {
        // Arrange
        var method = typeof(JobWizardViewModel).GetMethod("GetLocalizedValue", BindingFlags.NonPublic | BindingFlags.Instance);
        
        var jsonString = "\"Plain String\"";
        var node = System.Text.Json.Nodes.JsonNode.Parse(jsonString);

        // Act & Assert
        var result = method.Invoke(_viewModel, new object[] { node, "bg", "en" });
        result.Should().Be("Plain String");
    }

    private void SetWizardSteps(JobWizardViewModel vm, List<WizardStep> steps)
    {
        var field = typeof(JobWizardViewModel).GetField("_wizardSteps", BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(vm, steps);
    }
}