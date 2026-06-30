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
        mockCategoryNode.Setup(c => c.Type).Returns(CategoryType.CategorySpecific);

        var categoryVm = new SelectableCategoryViewModel(mockCategoryNode.Object) { IsSelected = true };
        _viewModel.SelectableCategories = new System.Collections.ObjectModel.ObservableCollection<SelectableCategoryViewModel> { categoryVm };

        var field = typeof(JobWizardViewModel).GetField("_currentProjectId", BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(_viewModel, null);

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

        var createProjectQueryMock = new Mock<ICreateProjectMutation>();
        var createProjectResponseMock = new Mock<IOperationResult<ICreateProjectResult>>();
        var clientErrorMock = new Mock<IClientError>();
        clientErrorMock.Setup(e => e.Message).Returns("Mocked database error");
        createProjectResponseMock.Setup(r => r.Errors).Returns(new List<IClientError> { clientErrorMock.Object });
        createProjectQueryMock.Setup(q => q.ExecuteAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync(createProjectResponseMock.Object);
        _apiClientMock.Setup(a => a.CreateProject).Returns(createProjectQueryMock.Object);

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
        mockCategoryNode.Setup(c => c.Type).Returns(CategoryType.CategorySpecific);

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
    public async Task LoadCategoriesAsync_WhenUnauthenticated_AllowsGuestFlowAndSetsDefaultRequests()
    {
        // Arrange
        var apiClientMock = new Mock<IBuildSmartApiClient>();

        var userQueryMock = new Mock<IGetCurrentUserQuery>();
        var userResponseMock = new Mock<IOperationResult<IGetCurrentUserResult>>();
        userResponseMock.Setup(r => r.Errors).Returns(new List<IClientError>());
        userResponseMock.Setup(r => r.Data).Returns((IGetCurrentUserResult?)null);
        userQueryMock.Setup(q => q.ExecuteAsync(default)).ReturnsAsync(userResponseMock.Object);
        apiClientMock.Setup(a => a.GetCurrentUser).Returns(userQueryMock.Object);

        var getCategoriesQuery = new Mock<IGetServiceCategoriesQuery>();
        var categoriesResultMock = new Mock<IGetServiceCategoriesResult>();
        categoriesResultMock.Setup(r => r.ServiceCategories).Returns(new List<IGetServiceCategories_ServiceCategories>());
        var categoriesResponseMock = new Mock<IOperationResult<IGetServiceCategoriesResult>>();
        categoriesResponseMock.Setup(r => r.Errors).Returns(new List<IClientError>());
        categoriesResponseMock.Setup(r => r.Data).Returns(categoriesResultMock.Object);
        getCategoriesQuery.Setup(q => q.ExecuteAsync(default)).ReturnsAsync(categoriesResponseMock.Object);
        apiClientMock.Setup(a => a.GetServiceCategories).Returns(getCategoriesQuery.Object);

        var mockNavigation = new Mock<INavigationBridge>();
        var mockAlerts = new Mock<IAlertService>();
        AppServiceLocator.Navigation = mockNavigation.Object;
        AppServiceLocator.Alerts = mockAlerts.Object;

        // Act
        var viewModel = new JobWizardViewModel(apiClientMock.Object);
        await viewModel.LoadCategoriesAsync();

        // Assert
        viewModel.RemainingAiRequests.Should().Be(20);
        mockNavigation.Verify(x => x.NavigateToAsync("..", null), Times.Never);
    }

    [Fact]
    public async Task CategorySelection_WhenExceedsRemainingAiRequests_RevertsIsSelected()
    {
        // Arrange
        var apiClientMock = new Mock<IBuildSmartApiClient>();

        var mockCategoryNode = new Mock<IGetServiceCategories_ServiceCategories>();
        mockCategoryNode.Setup(c => c.Id).Returns(Guid.NewGuid());
        mockCategoryNode.Setup(c => c.Name).Returns("Electrical");
        mockCategoryNode.Setup(c => c.Type).Returns(CategoryType.CategorySpecific);

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

    [Fact]
    public async Task SaveDraft_ShouldPassCategoryNameAsDescription_NotGlobalProjectDescription()
    {
        // Arrange
        var mockCategoryNode = new Mock<IGetServiceCategories_ServiceCategories>();
        var catId = Guid.NewGuid();
        mockCategoryNode.Setup(c => c.Id).Returns(catId);
        mockCategoryNode.Setup(c => c.Name).Returns("Electrical Category Name");
        mockCategoryNode.Setup(c => c.Type).Returns(CategoryType.CategorySpecific);

        // Mock GetCategories to return our category
        var getCategoriesQuery = new Mock<IGetServiceCategoriesQuery>();
        var categoriesResultMock = new Mock<IGetServiceCategoriesResult>();
        categoriesResultMock.Setup(r => r.ServiceCategories).Returns(new List<IGetServiceCategories_ServiceCategories> { mockCategoryNode.Object });
        var categoriesResponseMock = new Mock<IOperationResult<IGetServiceCategoriesResult>>();
        categoriesResponseMock.Setup(r => r.Errors).Returns(new List<IClientError>());
        categoriesResponseMock.Setup(r => r.Data).Returns(categoriesResultMock.Object);
        getCategoriesQuery.Setup(q => q.ExecuteAsync(default)).ReturnsAsync(categoriesResponseMock.Object);
        _apiClientMock.Setup(a => a.GetServiceCategories).Returns(getCategoriesQuery.Object);

        // Mock GetMyProjects to succeed with empty projects (so LoadCategoriesAsync doesn't crash)
        var getProjectsQuery = new Mock<IGetMyProjectsQuery>();
        var projectsResponseMock = new Mock<IOperationResult<IGetMyProjectsResult>>();
        projectsResponseMock.Setup(r => r.Errors).Returns(new List<IClientError>());
        var projectsResultDataMock = new Mock<IGetMyProjectsResult>();
        projectsResultDataMock.Setup(d => d.MyProjects).Returns(new List<IGetMyProjects_MyProjects>());
        projectsResponseMock.Setup(r => r.Data).Returns(projectsResultDataMock.Object);
        getProjectsQuery.Setup(q => q.ExecuteAsync(default)).ReturnsAsync(projectsResponseMock.Object);
        _apiClientMock.Setup(a => a.GetMyProjects).Returns(getProjectsQuery.Object);

        // Mock UpdateProjectDetails mutation
        var updateProjectDetailsMock = new Mock<IUpdateProjectDetailsMutation>();
        var updateResponseMock = new Mock<IOperationResult<IUpdateProjectDetailsResult>>();
        updateResponseMock.Setup(r => r.Errors).Returns(new List<IClientError>());
        updateProjectDetailsMock.Setup(q => q.ExecuteAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), default))
            .ReturnsAsync(updateResponseMock.Object);
        _apiClientMock.Setup(a => a.UpdateProjectDetails).Returns(updateProjectDetailsMock.Object);

        // Mock SaveJobPostDraft mutation
        var saveJobPostDraftMock = new Mock<ISaveJobPostDraftMutation>();
        var saveJobPostResponseMock = new Mock<IOperationResult<ISaveJobPostDraftResult>>();
        saveJobPostResponseMock.Setup(r => r.Errors).Returns(new List<IClientError>());
        
        string? passedDescription = null;
        saveJobPostDraftMock.Setup(q => q.ExecuteAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal?>(), It.IsAny<string>(), default))
            .Callback<Guid, string, string, string, decimal?, string, System.Threading.CancellationToken>((id, details, desc, loc, budget, cur, token) => passedDescription = desc)
            .ReturnsAsync(saveJobPostResponseMock.Object);
        _apiClientMock.Setup(a => a.SaveJobPostDraft).Returns(saveJobPostDraftMock.Object);

        var mockAlerts = new Mock<IAlertService>();
        AppServiceLocator.Alerts = mockAlerts.Object;

        await _viewModel.LoadCategoriesAsync();

        // Set private fields via reflection first
        var projectIdField = typeof(JobWizardViewModel).GetField("_currentProjectId", BindingFlags.NonPublic | BindingFlags.Instance);
        var projectId = Guid.NewGuid();
        projectIdField?.SetValue(_viewModel, projectId);

        var jobPostIdsField = typeof(JobWizardViewModel).GetField("_currentJobPostIds", BindingFlags.NonPublic | BindingFlags.Instance);
        var jobPostIds = new Dictionary<Guid, Guid> { { catId, Guid.NewGuid() } };
        jobPostIdsField?.SetValue(_viewModel, jobPostIds);

        // Setup Project details and selected categories
        var catVm = _viewModel.SelectableCategories[0];
        catVm.IsSelected = true;
        
        _viewModel.ProjectTitle = "Global Project Title";
        _viewModel.ProjectDescription = "Global Project Description";
        _viewModel.ProjectLocation = "Sofia";

        // Act
        var result = await _viewModel.SaveDraftAsync();

        // Assert
        result.Should().BeTrue();
        passedDescription.Should().Be("Electrical Category Name"); // It should not be "Global Project Description"
    }

    [Fact]
    public void QuestionAnswerChange_ShouldSyncProjLocationToProjectLocation()
    {
        // Arrange
        var q = new WizardQuestionViewModel { Id = "proj_location", Type = "text", Answer = "" };
        var step = new WizardStep { Type = WizardStepType.Info, Title = "Project Details" };
        step.Questions.Add(q);
        
        SetWizardSteps(_viewModel, new List<WizardStep> { step });
        
        var method = typeof(JobWizardViewModel).GetMethod("LoadStepData", BindingFlags.NonPublic | BindingFlags.Instance);
        method!.Invoke(_viewModel, new object[] { 0 });

        // Act
        q.Answer = "Plovdiv";

        // Assert
        _viewModel.ProjectLocation.Should().Be("Plovdiv");
    }

    [Fact]
    public void LoadStepData_ShouldSyncProjectLocationToProjLocationQuestionAnswer()
    {
        // Arrange
        _viewModel.ProjectLocation = "Burgas";
        var q = new WizardQuestionViewModel { Id = "proj_location", Type = "text", Answer = "" };
        var step = new WizardStep { Type = WizardStepType.Info, Title = "Project Details" };
        step.Questions.Add(q);
        
        SetWizardSteps(_viewModel, new List<WizardStep> { step });
        
        var method = typeof(JobWizardViewModel).GetMethod("LoadStepData", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        method!.Invoke(_viewModel, new object[] { 0 });

        // Assert
        q.Answer.Should().Be("Burgas");
        
        var masterAnswersField = typeof(JobWizardViewModel).GetField("_masterAnswerKey", BindingFlags.NonPublic | BindingFlags.Instance);
        var masterAnswers = masterAnswersField?.GetValue(_viewModel) as Dictionary<string, string>;
        masterAnswers.Should().ContainKey("proj_location");
        masterAnswers?["proj_location"].Should().Be("Burgas");
    }

    [Fact]
    public async Task LoadExistingProject_ShouldInitializeProjectLocation_FromMasterAnswerKey()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var jobPostId = Guid.NewGuid();
        var targetCatId = Guid.NewGuid();
        
        // Mock GetMyProjects
        var getProjectsQuery = new Mock<IGetMyProjectsQuery>();
        var projectsResponseMock = new Mock<IOperationResult<IGetMyProjectsResult>>();
        projectsResponseMock.Setup(r => r.Errors).Returns(new List<IClientError>());
        
        var resultDataMock = new Mock<IGetMyProjectsResult>();
        var projectMock = new Mock<IGetMyProjects_MyProjects>();
        projectMock.Setup(p => p.Id).Returns(projectId);
        projectMock.Setup(p => p.Title).Returns("Existing Title");
        projectMock.Setup(p => p.Description).Returns("Existing Description");
        projectMock.Setup(p => p.Status).Returns(ProjectStatus.Draft);
        projectMock.Setup(p => p.LastVisitedStep).Returns(0);
        
        var categoryMock = new Mock<IGetProjectsForReview_ProjectsForReview_JobPosts_ServiceCategory>();
        categoryMock.Setup(c => c.Id).Returns(targetCatId);
        
        var jobPostMock = new Mock<IGetProjectsForReview_ProjectsForReview_JobPosts>();
        jobPostMock.Setup(j => j.Id).Returns(jobPostId);
        jobPostMock.Setup(j => j.Location).Returns("Sofia"); // JobPost location is Sofia
        jobPostMock.Setup(j => j.JobDetails).Returns("{\"proj_location\":\"Varna\"}"); // Answer key location is Varna!
        jobPostMock.Setup(j => j.ServiceCategory).Returns(categoryMock.Object);
        
        projectMock.Setup(p => p.JobPosts).Returns(new List<IGetProjectsForReview_ProjectsForReview_JobPosts> { jobPostMock.Object });
        resultDataMock.Setup(d => d.MyProjects).Returns(new List<IGetMyProjects_MyProjects> { projectMock.Object });
        projectsResponseMock.Setup(r => r.Data).Returns(resultDataMock.Object);
        getProjectsQuery.Setup(q => q.ExecuteAsync(default)).ReturnsAsync(projectsResponseMock.Object);
        _apiClientMock.Setup(a => a.GetMyProjects).Returns(getProjectsQuery.Object);

        // Act
        var method = typeof(JobWizardViewModel).GetMethod("LoadExistingProjectAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        await (Task)method!.Invoke(_viewModel, new object[] { projectId })!;

        // Assert
        _viewModel.ProjectLocation.Should().Be("Varna"); // Should use the answer key location "Varna" over job location "Sofia"!
    }

    [Fact]
    public async Task GenerateDynamicSteps_WhenUnauthenticated_ShouldIncludeUserInformationStep()
    {
        // Arrange
        var apiClientMock = new Mock<IBuildSmartApiClient>();

        // Mock GetCurrentUser to fail/unauthenticated
        var userQueryMock = new Mock<IGetCurrentUserQuery>();
        var userResponseMock = new Mock<IOperationResult<IGetCurrentUserResult>>();
        userResponseMock.Setup(r => r.Errors).Returns(new List<IClientError>());
        userResponseMock.Setup(r => r.Data).Returns((IGetCurrentUserResult?)null);
        userQueryMock.Setup(q => q.ExecuteAsync(default)).ReturnsAsync(userResponseMock.Object);
        apiClientMock.Setup(a => a.GetCurrentUser).Returns(userQueryMock.Object);

        // Mock categories (including a UserType category)
        var userCategoryMock = new Mock<IGetServiceCategories_ServiceCategories>();
        userCategoryMock.Setup(c => c.Id).Returns(Guid.NewGuid());
        userCategoryMock.Setup(c => c.Name).Returns("User Info");
        userCategoryMock.Setup(c => c.Type).Returns(CategoryType.UserType);
        userCategoryMock.Setup(c => c.TemplateStructure).Returns("{\"questions\": [{\"id\":\"user_name\", \"text\":\"Name\", \"type\":\"text\", \"required\":true}]}");

        var categoriesResultMock = new Mock<IGetServiceCategoriesResult>();
        categoriesResultMock.Setup(r => r.ServiceCategories).Returns(new List<IGetServiceCategories_ServiceCategories> { userCategoryMock.Object });
        var categoriesResponseMock = new Mock<IOperationResult<IGetServiceCategoriesResult>>();
        categoriesResponseMock.Setup(r => r.Errors).Returns(new List<IClientError>());
        categoriesResponseMock.Setup(r => r.Data).Returns(categoriesResultMock.Object);
        var getCategoriesQuery = new Mock<IGetServiceCategoriesQuery>();
        getCategoriesQuery.Setup(q => q.ExecuteAsync(default)).ReturnsAsync(categoriesResponseMock.Object);
        apiClientMock.Setup(a => a.GetServiceCategories).Returns(getCategoriesQuery.Object);

        var viewModel = new JobWizardViewModel(apiClientMock.Object);
        await viewModel.LoadCategoriesAsync();

        // Act
        var method = typeof(JobWizardViewModel).GetMethod("GenerateDynamicSteps", BindingFlags.NonPublic | BindingFlags.Instance);
        await (Task)method!.Invoke(viewModel, null)!;

        // Assert
        viewModel.WizardSteps.Should().Contain(step => step.Title == "User Information" && step.Type == WizardStepType.Questions);
    }

    [Fact]
    public async Task GenerateDynamicSteps_WhenAuthenticated_ShouldNotIncludeUserInformationStep()
    {
        // Arrange
        var apiClientMock = new Mock<IBuildSmartApiClient>();

        // Mock GetCurrentUser to succeed
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

        // Mock categories (including a UserType category)
        var userCategoryMock = new Mock<IGetServiceCategories_ServiceCategories>();
        userCategoryMock.Setup(c => c.Id).Returns(Guid.NewGuid());
        userCategoryMock.Setup(c => c.Name).Returns("User Info");
        userCategoryMock.Setup(c => c.Type).Returns(CategoryType.UserType);
        userCategoryMock.Setup(c => c.TemplateStructure).Returns("{\"questions\": [{\"id\":\"user_name\", \"text\":\"Name\", \"type\":\"text\", \"required\":true}]}");

        var categoriesResultMock = new Mock<IGetServiceCategoriesResult>();
        categoriesResultMock.Setup(r => r.ServiceCategories).Returns(new List<IGetServiceCategories_ServiceCategories> { userCategoryMock.Object });
        var categoriesResponseMock = new Mock<IOperationResult<IGetServiceCategoriesResult>>();
        categoriesResponseMock.Setup(r => r.Errors).Returns(new List<IClientError>());
        categoriesResponseMock.Setup(r => r.Data).Returns(categoriesResultMock.Object);
        var getCategoriesQuery = new Mock<IGetServiceCategoriesQuery>();
        getCategoriesQuery.Setup(q => q.ExecuteAsync(default)).ReturnsAsync(categoriesResponseMock.Object);
        apiClientMock.Setup(a => a.GetServiceCategories).Returns(getCategoriesQuery.Object);

        var viewModel = new JobWizardViewModel(apiClientMock.Object);
        await viewModel.LoadCategoriesAsync();

        // Act
        var method = typeof(JobWizardViewModel).GetMethod("GenerateDynamicSteps", BindingFlags.NonPublic | BindingFlags.Instance);
        await (Task)method!.Invoke(viewModel, null)!;

        // Assert
        viewModel.WizardSteps.Should().NotContain(step => step.Title == "User Information");
    }

    private void SetWizardSteps(JobWizardViewModel vm, List<WizardStep> steps)
    {
        var field = typeof(JobWizardViewModel).GetField("_wizardSteps", BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(vm, steps);
    }
}