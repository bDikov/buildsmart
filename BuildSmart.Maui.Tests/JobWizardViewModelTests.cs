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
using BuildSmart.SharedUI.Services;

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

        // Mock default authenticated user for initialization
        var userQueryMock = new Mock<IGetCurrentUserQuery>();
        var userResponseMock = new Mock<IOperationResult<IGetCurrentUserResult>>();
        userResponseMock.Setup(r => r.Errors).Returns(new List<IClientError>());

        var userMock = new Mock<IGetCurrentUser_CurrentUser>();
        userMock.Setup(u => u.Id).Returns(Guid.NewGuid().ToString());
        userMock.Setup(u => u.RemainingAiRequests).Returns(20);

        var userResultMock = new Mock<IGetCurrentUserResult>();
        userResultMock.Setup(r => r.CurrentUser).Returns(userMock.Object);
        userResponseMock.Setup(r => r.Data).Returns(userResultMock.Object);

        userQueryMock.Setup(q => q.ExecuteAsync(default)).ReturnsAsync(userResponseMock.Object);
        _apiClientMock.Setup(a => a.GetCurrentUser).Returns(userQueryMock.Object);
        
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
        Assert.NotNull(method);
        
        // Act 1: Answer Q1 to show Q2
        q1.Answer = "OptionA";
        method!.Invoke(_viewModel, null);
        
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
        Assert.NotNull(method);
        
        var jsonString = "{ \"bg\": \"Здравей\", \"en\": \"Hello\" }";
        var node = System.Text.Json.Nodes.JsonNode.Parse(jsonString);
        Assert.NotNull(node);

        // Act & Assert
        var resultBg = method!.Invoke(_viewModel, new object?[] { node, "bg", "en" }) as string;
        resultBg.Should().Be("Здравей");

        var resultEn = method.Invoke(_viewModel, new object?[] { node, "en", "bg" }) as string;
        resultEn.Should().Be("Hello");

        var resultFallback = method.Invoke(_viewModel, new object?[] { node, "fr", "en" }) as string;
        resultFallback.Should().Be("Hello");
    }

    [Fact]
    public void GetLocalizedValue_WithPlainString_ReturnsString()
    {
        // Arrange
        var method = typeof(JobWizardViewModel).GetMethod("GetLocalizedValue", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        
        var jsonString = "\"Plain String\"";
        var node = System.Text.Json.Nodes.JsonNode.Parse(jsonString);
        Assert.NotNull(node);

        // Act & Assert
        var result = method!.Invoke(_viewModel, new object?[] { node, "bg", "en" }) as string;
        result.Should().Be("Plain String");
    }

    [Fact]
    public async Task GoToNextStep_WhenSaveDraftFails_DoesNotNavigate()
    {
        // Arrange
        _viewModel.CurrentStep = 0;
        var steps = new List<WizardStep>
        {
            new WizardStep { Type = WizardStepType.CategorySelection, Title = "Select Categories" },
            new WizardStep { Type = WizardStepType.Review, Title = "Review & Submit" }
        };
        SetWizardSteps(_viewModel, steps);

        var mockCategoryNode = new Mock<IGetServiceCategories_ServiceCategories>();
        mockCategoryNode.Setup(c => c.Id).Returns(Guid.NewGuid());
        mockCategoryNode.Setup(c => c.Name).Returns("Electrical");

        var categoryVm = new SelectableCategoryViewModel(mockCategoryNode.Object) { IsSelected = true };
        _viewModel.SelectableCategories = new System.Collections.ObjectModel.ObservableCollection<SelectableCategoryViewModel> { categoryVm };

        var field = typeof(JobWizardViewModel).GetField("_currentProjectId", BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(_viewModel, null);

        var userQueryMock = new Mock<IGetCurrentUserQuery>();
        var userResponseMock = new Mock<IOperationResult<IGetCurrentUserResult>>();
        var clientErrorMock = new Mock<IClientError>();
        clientErrorMock.Setup(e => e.Message).Returns("Mocked auth error");
        userResponseMock.Setup(r => r.Errors).Returns(new List<IClientError> { clientErrorMock.Object });
        userQueryMock.Setup(q => q.ExecuteAsync(default)).ReturnsAsync(userResponseMock.Object);
        _apiClientMock.Setup(a => a.GetCurrentUser).Returns(userQueryMock.Object);

        // Act
        await _viewModel.GoToNextStep();

        // Assert
        _viewModel.CurrentStep.Should().Be(0);
    }

    [Fact]
    public async Task CategorySelection_WhenSaveDraftFails_RevertsIsSelected()
    {
        // Arrange
        var apiClientMock = new Mock<IBuildSmartApiClient>();

        var mockCategoryNode = new Mock<IGetServiceCategories_ServiceCategories>();
        mockCategoryNode.Setup(c => c.Id).Returns(Guid.NewGuid());
        mockCategoryNode.Setup(c => c.Name).Returns("Electrical");

        var categoriesResultMock = new Mock<IGetServiceCategoriesResult>();
        categoriesResultMock.Setup(r => r.ServiceCategories).Returns(new List<IGetServiceCategories_ServiceCategories> { mockCategoryNode.Object });

        var categoriesResponseMock = new Mock<IOperationResult<IGetServiceCategoriesResult>>();
        categoriesResponseMock.Setup(r => r.Errors).Returns(new List<IClientError>());
        categoriesResponseMock.Setup(r => r.Data).Returns(categoriesResultMock.Object);

        var getCategoriesQuery = new Mock<IGetServiceCategoriesQuery>();
        getCategoriesQuery.Setup(q => q.ExecuteAsync(default)).ReturnsAsync(categoriesResponseMock.Object);
        apiClientMock.Setup(a => a.GetServiceCategories).Returns(getCategoriesQuery.Object);

        // Mock GetCurrentUser to succeed initially during load
        var userQueryMock = new Mock<IGetCurrentUserQuery>();
        var userResponseMock = new Mock<IOperationResult<IGetCurrentUserResult>>();
        userResponseMock.Setup(r => r.Errors).Returns(new List<IClientError>());

        var userMock = new Mock<IGetCurrentUser_CurrentUser>();
        userMock.Setup(u => u.Id).Returns(Guid.NewGuid().ToString());
        userMock.Setup(u => u.RemainingAiRequests).Returns(20);

        var userResultMock = new Mock<IGetCurrentUserResult>();
        userResultMock.Setup(r => r.CurrentUser).Returns(userMock.Object);
        userResponseMock.Setup(r => r.Data).Returns(userResultMock.Object);

        userQueryMock.Setup(q => q.ExecuteAsync(default)).ReturnsAsync(userResponseMock.Object);
        apiClientMock.Setup(a => a.GetCurrentUser).Returns(userQueryMock.Object);

        // Mock CreateProject to return errors (this simulates saving the draft failing)
        var createProjectQueryMock = new Mock<ICreateProjectMutation>();
        var createProjectResponseMock = new Mock<IOperationResult<ICreateProjectResult>>();
        var clientErrorMock = new Mock<IClientError>();
        clientErrorMock.Setup(e => e.Message).Returns("Mocked database error");
        createProjectResponseMock.Setup(r => r.Errors).Returns(new List<IClientError> { clientErrorMock.Object });
        createProjectQueryMock.Setup(q => q.ExecuteAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync(createProjectResponseMock.Object);
        apiClientMock.Setup(a => a.CreateProject).Returns(createProjectQueryMock.Object);

        var mockAlerts = new Mock<IAlertService>();
        AppServiceLocator.Alerts = mockAlerts.Object;

        var viewModel = new JobWizardViewModel(apiClientMock.Object);
        await viewModel.LoadCategoriesAsync();

        var categoryVm = viewModel.SelectableCategories[0];
        categoryVm.IsSelected.Should().BeFalse();

        // Act
        categoryVm.IsSelected = true;
        await Task.Delay(100);

        // Assert
        categoryVm.IsSelected.Should().BeFalse();
    }

    [Fact]
    public async Task LoadCategoriesAsync_WhenUnauthenticated_AbortsAndAlerts()
    {
        // Arrange
        var apiClientMock = new Mock<IBuildSmartApiClient>();

        var userQueryMock = new Mock<IGetCurrentUserQuery>();
        var userResponseMock = new Mock<IOperationResult<IGetCurrentUserResult>>();
        userResponseMock.Setup(r => r.Errors).Returns(new List<IClientError>());
        userResponseMock.Setup(r => r.Data).Returns((IGetCurrentUserResult?)null);
        userQueryMock.Setup(q => q.ExecuteAsync(default)).ReturnsAsync(userResponseMock.Object);
        apiClientMock.Setup(a => a.GetCurrentUser).Returns(userQueryMock.Object);

        var mockNavigation = new Mock<INavigationBridge>();
        var mockAlerts = new Mock<IAlertService>();
        AppServiceLocator.Navigation = mockNavigation.Object;
        AppServiceLocator.Alerts = mockAlerts.Object;

        // Act
        var viewModel = new JobWizardViewModel(apiClientMock.Object);
        await Task.Delay(100);

        // Assert
        mockAlerts.Verify(x => x.DisplayAlert(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        mockNavigation.Verify(x => x.NavigateToAsync("..", null), Times.Once);
    }

    [Fact]
    public async Task CategorySelection_WhenExceedsRemainingAiRequests_RevertsIsSelected()
    {
        // Arrange
        var apiClientMock = new Mock<IBuildSmartApiClient>();

        var mockCategoryNode = new Mock<IGetServiceCategories_ServiceCategories>();
        mockCategoryNode.Setup(c => c.Id).Returns(Guid.NewGuid());
        mockCategoryNode.Setup(c => c.Name).Returns("Electrical");

        var categoriesResultMock = new Mock<IGetServiceCategoriesResult>();
        categoriesResultMock.Setup(r => r.ServiceCategories).Returns(new List<IGetServiceCategories_ServiceCategories> { mockCategoryNode.Object });

        var categoriesResponseMock = new Mock<IOperationResult<IGetServiceCategoriesResult>>();
        categoriesResponseMock.Setup(r => r.Errors).Returns(new List<IClientError>());
        categoriesResponseMock.Setup(r => r.Data).Returns(categoriesResultMock.Object);

        var getCategoriesQuery = new Mock<IGetServiceCategoriesQuery>();
        getCategoriesQuery.Setup(q => q.ExecuteAsync(default)).ReturnsAsync(categoriesResponseMock.Object);
        apiClientMock.Setup(a => a.GetServiceCategories).Returns(getCategoriesQuery.Object);

        // Mock GetCurrentUser to succeed but have 0 remaining requests
        var userQueryMock = new Mock<IGetCurrentUserQuery>();
        var userResponseMock = new Mock<IOperationResult<IGetCurrentUserResult>>();
        userResponseMock.Setup(r => r.Errors).Returns(new List<IClientError>());

        var userMock = new Mock<IGetCurrentUser_CurrentUser>();
        userMock.Setup(u => u.Id).Returns(Guid.NewGuid().ToString());
        userMock.Setup(u => u.RemainingAiRequests).Returns(0); // 0 remaining requests!

        var userResultMock = new Mock<IGetCurrentUserResult>();
        userResultMock.Setup(r => r.CurrentUser).Returns(userMock.Object);
        userResponseMock.Setup(r => r.Data).Returns(userResultMock.Object);

        userQueryMock.Setup(q => q.ExecuteAsync(default)).ReturnsAsync(userResponseMock.Object);
        apiClientMock.Setup(a => a.GetCurrentUser).Returns(userQueryMock.Object);

        var mockAlerts = new Mock<IAlertService>();
        AppServiceLocator.Alerts = mockAlerts.Object;

        var viewModel = new JobWizardViewModel(apiClientMock.Object);
        await viewModel.LoadCategoriesAsync();

        var categoryVm = viewModel.SelectableCategories[0];
        categoryVm.IsSelected.Should().BeFalse();

        // Act
        categoryVm.IsSelected = true;
        await Task.Delay(100);

        // Assert
        categoryVm.IsSelected.Should().BeFalse(); // Reverted because 1 > 0
        mockAlerts.Verify(x => x.DisplayAlert(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void ValidateCategoryStep_WhenExceedsRemainingAiRequests_ReturnsFalseAndAlerts()
    {
        // Arrange
        var apiClientMock = new Mock<IBuildSmartApiClient>();

        // Mock GetCategories to succeed (prevent NRE in constructor background task)
        var categoriesResultMock = new Mock<IGetServiceCategoriesResult>();
        categoriesResultMock.Setup(r => r.ServiceCategories).Returns(new List<IGetServiceCategories_ServiceCategories>());
        var categoriesResponseMock = new Mock<IOperationResult<IGetServiceCategoriesResult>>();
        categoriesResponseMock.Setup(r => r.Errors).Returns(new List<IClientError>());
        categoriesResponseMock.Setup(r => r.Data).Returns(categoriesResultMock.Object);
        var getCategoriesQuery = new Mock<IGetServiceCategoriesQuery>();
        getCategoriesQuery.Setup(q => q.ExecuteAsync(default)).ReturnsAsync(categoriesResponseMock.Object);
        apiClientMock.Setup(a => a.GetServiceCategories).Returns(getCategoriesQuery.Object);

        // Mock default authenticated user with 1 remaining request
        var userQueryMock = new Mock<IGetCurrentUserQuery>();
        var userResponseMock = new Mock<IOperationResult<IGetCurrentUserResult>>();
        userResponseMock.Setup(r => r.Errors).Returns(new List<IClientError>());

        var userMock = new Mock<IGetCurrentUser_CurrentUser>();
        userMock.Setup(u => u.Id).Returns(Guid.NewGuid().ToString());
        userMock.Setup(u => u.RemainingAiRequests).Returns(1); // 1 remaining request!

        var userResultMock = new Mock<IGetCurrentUserResult>();
        userResultMock.Setup(r => r.CurrentUser).Returns(userMock.Object);
        userResponseMock.Setup(r => r.Data).Returns(userResultMock.Object);

        userQueryMock.Setup(q => q.ExecuteAsync(default)).ReturnsAsync(userResponseMock.Object);
        apiClientMock.Setup(a => a.GetCurrentUser).Returns(userQueryMock.Object);

        var mockAlerts = new Mock<IAlertService>();
        AppServiceLocator.Alerts = mockAlerts.Object;

        var viewModel = new JobWizardViewModel(apiClientMock.Object);
        viewModel.RemainingAiRequests = 1;

        var cat1 = new SelectableCategoryViewModel(new Mock<IGetServiceCategories_ServiceCategories>().Object) { IsSelected = true };
        var cat2 = new SelectableCategoryViewModel(new Mock<IGetServiceCategories_ServiceCategories>().Object) { IsSelected = true };

        viewModel.SelectableCategories.Add(cat1);
        viewModel.SelectableCategories.Add(cat2); // 2 selected, which exceeds 1

        // Act
        var method = typeof(JobWizardViewModel).GetMethod("ValidateCategoryStep", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (bool?)method?.Invoke(viewModel, null);

        // Assert
        result.Should().BeFalse();
        mockAlerts.Verify(x => x.DisplayAlert(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    private void SetWizardSteps(JobWizardViewModel vm, List<WizardStep> steps)
    {
        var field = typeof(JobWizardViewModel).GetField("_wizardSteps", BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(vm, steps);
    }
}