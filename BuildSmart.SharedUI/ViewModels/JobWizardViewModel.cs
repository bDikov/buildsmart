using BuildSmart.SharedUI.Services;
using BuildSmart.SharedUI.MauiMocks;
using BuildSmart.SharedUI.GraphQL;
using Microsoft.Extensions.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BuildSmart.SharedUI.ViewModels;

public partial class JobWizardViewModel : ObservableObject, IQueryAttributable
{
	private readonly IBuildSmartApiClient _apiClient;
	private readonly System.Threading.SemaphoreSlim _saveLock = new(1, 1);
	private System.Threading.CancellationTokenSource? _saveDebounceCts;
	private bool _isUpdatingSelection;

	// --- Steps & Visibility ---
	private List<WizardStep> _wizardSteps = new();
	public IReadOnlyList<WizardStep> WizardSteps => _wizardSteps;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsInfoStepVisible))]
	[NotifyPropertyChangedFor(nameof(IsCategoryStepVisible))]
	[NotifyPropertyChangedFor(nameof(IsQuestionStepVisible))]
	[NotifyPropertyChangedFor(nameof(IsReviewStepVisible))]
	[NotifyPropertyChangedFor(nameof(CurrentStepTitle))]
	[NotifyPropertyChangedFor(nameof(NextButtonText))]
	[NotifyPropertyChangedFor(nameof(TotalSteps))]
	[NotifyPropertyChangedFor(nameof(CurrentStepNumber))]
	[NotifyPropertyChangedFor(nameof(ProgressPercentage))]
	private int _currentStep = 0;

	partial void OnCurrentStepChanged(int value)
	{
		if (IsBusy) return;
		TriggerDebouncedSave();
	}

	public int TotalSteps => _wizardSteps.Count;
	public int CurrentStepNumber => CurrentStep + 1;

	private double GetTextProgress(string? text, int targetLength, double maxPoints)
	{
		if (string.IsNullOrWhiteSpace(text)) return 0;
		double ratio = (double)text.Length / targetLength;
		return Math.Min(ratio, 1.0) * maxPoints;
	}

	public double ProgressPercentage 
	{
		get 
		{
			if (_wizardSteps.Count == 0) return 0;
			
			var stepType = _wizardSteps[CurrentStep].Type;
			if (stepType == WizardStepType.Review) return 70;
			
			if (stepType == WizardStepType.CategorySelection)
			{
				if (SelectableCategories != null && SelectableCategories.Any(c => c.IsSelected)) return 15;
				return 0;
			}

			if (stepType == WizardStepType.Info)
			{
				var visibleQuestionsInfo = Questions?.Where(q => q.IsVisible).ToList() ?? new List<WizardQuestionViewModel>();
				int totalQInfo = visibleQuestionsInfo.Count;
				double answeredQInfo = 0;
				if (totalQInfo > 0)
				{
					foreach (var q in visibleQuestionsInfo)
					{
						if (!string.IsNullOrWhiteSpace(q.Answer) && q.Answer != "False")
						{
							answeredQInfo += 1.0;
						}
					}
					return 70.0 + (30.0 * (answeredQInfo / totalQInfo));
				}
				return 70.0;
			}
			
			int questionStartIdx = _wizardSteps.FindIndex(s => s.Type == WizardStepType.Questions);
			int reviewIdx = _wizardSteps.FindIndex(s => s.Type == WizardStepType.Review);
			
			if (questionStartIdx == -1) questionStartIdx = 0;
			
			int totalQuestionSteps = _wizardSteps.Count - questionStartIdx;
			if (reviewIdx != -1) 
			{
			    totalQuestionSteps = reviewIdx - questionStartIdx;
			}
			
			int currentQuestionStep = CurrentStep - questionStartIdx;
			
			double baseProgress = 15.0;
			if (_wizardSteps.Count == 1 || questionStartIdx == 0) 
			{
			    baseProgress = 0.0; // Single step edit mode
			}

			double remainingProgress = 70.0 - baseProgress;
			if (_wizardSteps.Count == 1 || questionStartIdx == 0)
			{
				remainingProgress = 100.0; // Single step mode
			}
			
			// Calculate fraction of questions answered in this step
			var visibleQuestions = Questions?.Where(q => q.IsVisible).ToList() ?? new List<WizardQuestionViewModel>();
			int totalQ = visibleQuestions.Count;
			double answeredQ = 0;
			double mandatoryAnswered = 0;
			bool hasMandatory = false;
			
			if (totalQ > 0)
			{
				foreach (var q in visibleQuestions)
				{
					double qProg = 0;
					if (q.IsText)
					{
						qProg = GetTextProgress(q.Answer, 15, 1.0);
					}
					else if (q.IsBoolean)
					{
					    qProg = q.Answer == "True" ? 1.0 : 0.0;
					}
					else if (!string.IsNullOrWhiteSpace(q.Answer))
					{
						qProg = 1.0;
					}

					if (q.IsRequired)
					{
						hasMandatory = true;
						mandatoryAnswered += qProg;
					}
					
					answeredQ += qProg;
				}
			}
			
			if (hasMandatory && mandatoryAnswered == 0)
			{
				answeredQ = 0;
			}
			
			double stepFraction = totalQ > 0 ? answeredQ / totalQ : 1.0;
			
			int denominator = totalQuestionSteps;
			if (denominator <= 0) denominator = 1;
			double fraction = (currentQuestionStep + stepFraction) / denominator;
			
			return baseProgress + (remainingProgress * fraction);
		}
	}

	public bool IsInfoStepVisible => _wizardSteps.Any() && CurrentStep < _wizardSteps.Count && _wizardSteps[CurrentStep].Type == WizardStepType.Info;
	public bool IsCategoryStepVisible => _wizardSteps.Any() && CurrentStep < _wizardSteps.Count && _wizardSteps[CurrentStep].Type == WizardStepType.CategorySelection;
	public bool IsQuestionStepVisible => _wizardSteps.Any() && CurrentStep < _wizardSteps.Count && _wizardSteps[CurrentStep].Type == WizardStepType.Questions;
	public bool IsReviewStepVisible => _wizardSteps.Any() && CurrentStep < _wizardSteps.Count && _wizardSteps[CurrentStep].Type == WizardStepType.Review;

	public string CurrentStepTitle => _wizardSteps.Any() && CurrentStep < _wizardSteps.Count ? _wizardSteps[CurrentStep].Title : "";

	public string StepText => $"{CurrentStepNumber} of {TotalSteps} complete";

	public string NextButtonText => (IsEditing && CurrentStep == _wizardSteps.Count - 1) ? "Save & Re-generate" : "Next";

	// --- Data ---
	[ObservableProperty]
	private ObservableCollection<SelectableCategoryViewModel> _selectableCategories = new();

	private List<SelectableCategoryViewModel> _allCategories = new();

	[ObservableProperty]
	private string _projectTitle = string.Empty;
	partial void OnProjectTitleChanged(string value) => OnPropertyChanged(nameof(ProgressPercentage));

	[ObservableProperty]
	private string _projectDescription = string.Empty;
	partial void OnProjectDescriptionChanged(string value) => OnPropertyChanged(nameof(ProgressPercentage));

	[ObservableProperty]
	private string _projectLocation = string.Empty;
	partial void OnProjectLocationChanged(string value) => OnPropertyChanged(nameof(ProgressPercentage));

	[ObservableProperty]
	private DateTime? _preferredSiteVisitDate = null;
	partial void OnPreferredSiteVisitDateChanged(DateTime? value) => OnPropertyChanged(nameof(ProgressPercentage));

	// Errors
	[ObservableProperty] private bool _titleHasError;

	[ObservableProperty] private bool _descriptionHasError;
	[ObservableProperty] private bool _locationHasError;
	[ObservableProperty] private bool _categorySelectionHasError;

	// Key: QuestionId, Value: Answer
	private Dictionary<string, string> _masterAnswerKey = new();

	private Dictionary<string, string> _questionTextCache = new();

	[ObservableProperty]
	private ObservableCollection<WizardQuestionViewModel> _questions = new();

	[ObservableProperty]
	private bool _hasProjects = true; // Default to true, so swipe hint is hidden unless confirmed 0.

	[ObservableProperty]
	private bool _isBusy;

	[ObservableProperty]
	private bool _isEditing;

	[ObservableProperty]
	private bool _isOfferBuilding;

	[ObservableProperty]
	private int _remainingAiRequests = 20;

	public ObservableCollection<KeyValuePair<string, string>> AnswersList { get; } = new();

	private void RefreshAnswersList()
	{
		AnswersList.Clear();
		foreach (var kvp in _masterAnswerKey)
		{
			if (kvp.Key == null) continue;
			var text = _questionTextCache.TryGetValue(kvp.Key, out var qText) ? qText : kvp.Key;
			AnswersList.Add(new KeyValuePair<string, string>(text, kvp.Value ?? ""));
		}
	}

	private Guid? _currentProjectId;
	public Guid? CurrentProjectId => _currentProjectId;
	private Guid? _targetJobPostId;
	private Guid? _targetCategoryId;
	private Dictionary<Guid, Guid> _currentJobPostIds = new();
	private Dictionary<Guid, string> _lastSubmittedJobHashes = new();

	// Legacy property for backward compatibility if needed, but we use _masterAnswerKey now
	public Dictionary<string, object> WizardAnswers { get; private set; } = new();

	private Task? _loadCategoriesTask;
	private readonly SignalRService? _signalRService;
	private readonly IStringLocalizer<BuildSmart.SharedUI.Resources.AppResources>? _localizer;

	public JobWizardViewModel(
		IBuildSmartApiClient apiClient, 
		SignalRService? signalRService = null, 
		IStringLocalizer<BuildSmart.SharedUI.Resources.AppResources>? localizer = null)
	{
		_apiClient = apiClient;
		_signalRService = signalRService;
		_localizer = localizer;
		InitializeSteps();
		_loadCategoriesTask = LoadCategoriesAsync();
	}

	private void InitializeSteps()
	{
		_wizardSteps.Clear();
		_wizardSteps.Add(new WizardStep { Type = WizardStepType.CategorySelection, Title = _localizer?["JobWizard_SelectCategories"] ?? "Select Categories" });
		_wizardSteps.Add(new WizardStep { Type = WizardStepType.Review, Title = _localizer?["JobWizard_ReviewSubmit"] ?? "Review & Submit" });
		_wizardSteps.Add(new WizardStep { Type = WizardStepType.Info, Title = _localizer?["JobWizard_InfoStepTitle"] ?? "Project Details" });
	}

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (query == null) return;

		if (query.TryGetValue("JobPostId", out var jpid) && jpid != null && Guid.TryParse(jpid.ToString(), out var jobId))
			_targetJobPostId = jobId;

		if (query.TryGetValue("TargetCategoryId", out var tcid) && tcid != null && Guid.TryParse(tcid.ToString(), out var catId))
			_targetCategoryId = catId;

		if (query.TryGetValue("ProjectId", out var pidObj) && pidObj != null)
		{
			if (Guid.TryParse(pidObj.ToString(), out var projectId))
			{
				_currentProjectId = projectId;
				IsEditing = true;
				AppServiceLocator.MainThread.BeginInvokeOnMainThread(async () => await LoadExistingProjectAsync(projectId));
			}
		}
	}

	private async Task LoadExistingProjectAsync(Guid projectId)
	{
		try
		{
			IsBusy = true;
			var result = await _apiClient.GetMyProjects.ExecuteAsync();
			if (result.Data?.MyProjects != null)
			{
				var project = result.Data.MyProjects.FirstOrDefault(p => p.Id == projectId);
				if (project != null)
				{
					ProjectTitle = project.Title;
					ProjectDescription = project.Description;

					var firstJob = project.JobPosts.FirstOrDefault(j => j.Id == _targetJobPostId)
						?? project.JobPosts.FirstOrDefault();
					if (firstJob != null)
					{
						ProjectLocation = firstJob.Location ?? "";
					}

					var selectedCategoryIds = project.JobPosts.Select(j => j.ServiceCategory.Id).ToList();

					if (!_allCategories.Any())
					{
						await LoadCategoriesAsync();
					}

					foreach (var cat in SelectableCategories)
					{
						if (selectedCategoryIds.Contains(cat.Category.Id))
						{
							cat.IsSelected = true;
						}
					}

					_currentJobPostIds.Clear();
					foreach (var job in project.JobPosts)
					{
						if (job.ServiceCategory != null)
						{
							_currentJobPostIds[job.ServiceCategory.Id] = job.Id;
						}
					}

					// Generate Dynamic Steps based on loaded categories
					await GenerateDynamicSteps();

					// Pre-fill answers from all jobs in the project
					_masterAnswerKey.Clear();
					foreach (var job in project.JobPosts)
					{
						if (!string.IsNullOrEmpty(job.JobDetails))
						{
							try
							{
								var flatAnswers = JsonSerializer.Deserialize<Dictionary<string, string>>(job.JobDetails);
								if (flatAnswers != null)
								{
									foreach (var kvp in flatAnswers)
									{
										if (kvp.Key != null)
										{
											_masterAnswerKey[kvp.Key] = kvp.Value ?? "";
										}
									}
								}
							}
							catch { /* Ignore legacy format */ }
						}
					}

					if (_masterAnswerKey.TryGetValue("proj_location", out var savedLoc) && !string.IsNullOrWhiteSpace(savedLoc))
					{
						ProjectLocation = savedLoc;
					}

					// Position at the correct step
					if (_targetCategoryId != null)
					{
						CurrentStep = 0; // The only step in single-edit mode
					}
					else if (project.Status == ProjectStatus.Draft)
					{
						var lastStep = project.LastVisitedStep ?? 0;
						if (lastStep >= 0 && lastStep < _wizardSteps.Count)
						{
							CurrentStep = lastStep;
						}
						else
						{
							CurrentStep = 0;
						}
					}
					else if (selectedCategoryIds.Any() && _wizardSteps.Count > 2)
					{
						CurrentStep = 2; // Skip Info/Category in full project edit mode
					}

					LoadStepData(CurrentStep);
					RefreshVisibility();
				}
			}
		}
		catch (Exception ex)
		{
			await AppServiceLocator.Alerts.DisplayAlert("Error", $"Failed to load draft: {ex.Message}", "OK");
		}
		finally
		{
			IsBusy = false;
		}
	}

	private void RefreshVisibility()
	{
		OnPropertyChanged(nameof(IsInfoStepVisible));
		OnPropertyChanged(nameof(IsCategoryStepVisible));
		OnPropertyChanged(nameof(IsQuestionStepVisible));
		OnPropertyChanged(nameof(IsReviewStepVisible));
		OnPropertyChanged(nameof(CurrentStepTitle));
		OnPropertyChanged(nameof(NextButtonText));
	}

	[RelayCommand]
	public async Task LoadCategoriesAsync()
	{
		try
		{
			IsBusy = true;

			// Validate authentication: Guest user shouldn't be able to proceed/create drafts at all
			var userResult = await _apiClient.GetCurrentUser.ExecuteAsync();
			bool isGuest = userResult.Data?.CurrentUser?.Email?.EndsWith("@buildsmart.guest", StringComparison.OrdinalIgnoreCase) ?? false;
			if (userResult.Errors.Count > 0 || userResult.Data?.CurrentUser == null || isGuest)
			{
				string errorTitle = _localizer?["JobWizard_SubmissionError_Title"] ?? "Error";
				string okText = _localizer?["JobWizard_OK"] ?? "OK";
				string errorMsg = userResult.Errors.Count > 0 
					? userResult.Errors[0].Message 
					: (isGuest ? "Guest users cannot create standard projects. Please register." : "You must be logged in to create a project.");
				await AppServiceLocator.Alerts.DisplayAlert(errorTitle, errorMsg, okText);
				await AppServiceLocator.Navigation.NavigateToAsync("/login?ReturnUrl=%2fjob-wizard");
				return;
			}

			RemainingAiRequests = userResult.Data.CurrentUser.RemainingAiRequests;

			var result = await _apiClient.GetServiceCategories.ExecuteAsync();

			if (result.Errors.Count > 0)
			{
				var errorMessages = string.Join(", ", result.Errors.Select(e => e.Message));
				await AppServiceLocator.Alerts.DisplayAlert("GraphQL Error", errorMessages, "OK");
			}
			else if (result.Data?.ServiceCategories != null)
			{
				SelectableCategories.Clear();
				_allCategories.Clear();

				foreach (var cat in result.Data.ServiceCategories)
				{
					var viewModel = new SelectableCategoryViewModel(cat);
					viewModel.PropertyChanged += async (s, e) => 
					{
						if (e.PropertyName == nameof(SelectableCategoryViewModel.IsSelected))
						{
							OnPropertyChanged(nameof(ProgressPercentage));
							await HandleCategorySelectionChangedAsync(viewModel);
						}
					};
					
					_allCategories.Add(viewModel);

					if (!cat.IsGlobal && !IsProjectDetailsCategory(cat.TemplateStructure))
					{
						SelectableCategories.Add(viewModel);
					}
				}
				
				OnPropertyChanged(nameof(SelectableCategories));
			}

			// Also check if user has projects to determine if we should show the swipe hint
			try
			{
				var projectsResult = await _apiClient.GetMyProjects.ExecuteAsync();
				if (projectsResult.Data?.MyProjects != null)
				{
					HasProjects = projectsResult.Data.MyProjects.Any(p => p.Title != "Support Chat" && !p.Title.StartsWith("Support - "));
				}
			}
			catch { }
		}
		catch (Exception ex)
		{
			await AppServiceLocator.Alerts.DisplayAlert("Error", $"Failed to load categories: {ex.Message}", "OK");
		}
		finally
		{
			IsBusy = false;
		}
	}

	public bool ValidateCurrentStep()
	{
		if (CurrentStep >= _wizardSteps.Count) return false;
		var currentStepType = _wizardSteps[CurrentStep].Type;
		if (currentStepType == WizardStepType.Info) return ValidateQuestionsStep();
		if (currentStepType == WizardStepType.CategorySelection) return ValidateCategoryStep();
		if (currentStepType == WizardStepType.Questions) return ValidateQuestionsStep();
		return true;
	}

	[RelayCommand]
	public async Task GoToNextStep()
	{
		if (IsBusy || CurrentStep >= _wizardSteps.Count) return;

		try
		{
			IsBusy = true;
			var currentStepType = _wizardSteps[CurrentStep].Type;
			var currentStepIndex = CurrentStep;

			// 1. Validation & State Capture (Must stay on current page if fails)
			if (!ValidateCurrentStep()) return;

			if (currentStepType == WizardStepType.CategorySelection)
			{
				await GenerateDynamicSteps();
			}
			else if (currentStepType == WizardStepType.Questions)
			{
				// Save current questions to master key
				foreach (var q in Questions)
				{
					if (q.Id != null && !string.IsNullOrEmpty(q.Answer))
						_masterAnswerKey[q.Id] = q.Answer;
				}
			}

			// 2. SAVE DRAFT (Must succeed before navigating)
			bool saveSuccess = true;
			if (currentStepType == WizardStepType.CategorySelection)
			{
				saveSuccess = await InternalSaveDraftAsync(null, true);
			}
			else if (currentStepType == WizardStepType.Questions)
			{
				var stepTitle = _wizardSteps[currentStepIndex].Title;
				if (stepTitle == "General Questions")
				{
					saveSuccess = await InternalSaveDraftAsync(null, true);
				}
				else
				{
					var categoryName = stepTitle.Replace(" Questions", "");
					var cat = SelectableCategories.FirstOrDefault(c => c.Category.Name == categoryName);
					if (cat != null)
					{
						saveSuccess = await InternalSaveDraftAsync(cat);
						if (saveSuccess)
						{
							// Trigger AI Generation immediately for this category
							if (_currentJobPostIds.TryGetValue(cat.Category.Id, out var jobId))
							{
								var answersHash = JsonSerializer.Serialize(_masterAnswerKey);
								if (!_lastSubmittedJobHashes.TryGetValue(jobId, out var lastHash) || lastHash != answersHash)
								{
									var submitResult = await _apiClient.SubmitJobForScopeGeneration.ExecuteAsync(jobId);
									if (submitResult.Errors.Count > 0)
									{
										await AppServiceLocator.Alerts.DisplayAlert("Warning", $"Could not start AI for {categoryName}: {submitResult.Errors[0].Message}", "OK");
									}
									else
									{
										_lastSubmittedJobHashes[jobId] = answersHash;
									}
								}
							}
						}
					}
					else
					{
						saveSuccess = await InternalSaveDraftAsync();
					}
				}
			}

			if (!saveSuccess)
			{
				return;
			}

			// 3. NAVIGATE
			bool movedNext = false;
			if (CurrentStep < _wizardSteps.Count - 1)
			{
				CurrentStep++;
				LoadStepData(CurrentStep);
				movedNext = true;
			}

			if (!movedNext && IsEditing)
			{
				await SaveAndRegenerateAsync();
			}
		}
		catch (Exception ex)
		{
			await AppServiceLocator.Alerts.DisplayAlert("Error", ex.Message, "OK");
		}
		finally
		{
			IsBusy = false;
		}
	}

	private async Task SaveAndRegenerateAsync()
	{
		// IsBusy is already true from GoToNextStep
		try
		{
			if (!await InternalSaveDraftAsync())
			{
				return;
			}

			var jobsToRegenerate = _targetJobPostId != null
				? new List<Guid> { _targetJobPostId.Value }
				: _currentJobPostIds.Values.ToList();

			var actualRegenerateList = new List<Guid>();
			foreach (var jobId in jobsToRegenerate)
			{
				var answersHash = JsonSerializer.Serialize(_masterAnswerKey);
				if (!_lastSubmittedJobHashes.TryGetValue(jobId, out var lastHash) || lastHash != answersHash)
				{
					actualRegenerateList.Add(jobId);
				}
			}

			if (actualRegenerateList.Count > RemainingAiRequests)
			{
				string errorTitle = _localizer?["JobWizard_AiLimitReached_Title"] ?? "Limit reached";
				string okText = _localizer?["JobWizard_OK"] ?? "OK";
				string errorMsg = string.Format(_localizer?["JobWizard_AiRequestsExceeded"] ?? "Please select fewer categories to fit within your remaining monthly AI limit of {0} requests. You've selected {1} categories. You can also contact support to upgrade your account.", RemainingAiRequests, actualRegenerateList.Count);
				await AppServiceLocator.Alerts.DisplayAlert(errorTitle, errorMsg, okText);
				return;
			}

			foreach (var jobId in actualRegenerateList)
			{
				var answersHash = JsonSerializer.Serialize(_masterAnswerKey);
				var result = await _apiClient.SubmitJobForScopeGeneration.ExecuteAsync(jobId);
				if (result.Errors.Count > 0)
				{
					await AppServiceLocator.Alerts.DisplayAlert("Error", result.Errors[0].Message, "OK");
					return;
				}
				_lastSubmittedJobHashes[jobId] = answersHash;
			}

			await AppServiceLocator.Alerts.DisplayAlert("Success", "Answers updated. AI is re-generating your scope.", "OK");
			await AppServiceLocator.Navigation.NavigateToAsync(".."); 
		}
		catch (Exception ex)
		{
			await AppServiceLocator.Alerts.DisplayAlert("Error", ex.Message, "OK");
		}
	}

	[RelayCommand]
	public void GoToPreviousStep()
	{
		if (CurrentStep > 0)
		{
			if (CurrentStep < _wizardSteps.Count && _wizardSteps[CurrentStep].Type == WizardStepType.Questions)
			{
				foreach (var q in Questions)
				{
					if (q.Id != null)
						_masterAnswerKey[q.Id] = q.Answer ?? "";
				}
			}

			CurrentStep--;
			LoadStepData(CurrentStep);
		}
	}

	private void LoadStepData(int stepIndex)
	{
		var step = _wizardSteps[stepIndex];

		// Always refresh questions if it's a question step or project details info step
		if (step.Type == WizardStepType.Questions || step.Type == WizardStepType.Info)
		{
			foreach (var q in Questions)
			{
				q.PropertyChanged -= Question_PropertyChanged;
			}

			Questions.Clear();
			foreach (var q in step.Questions)
			{
				if (q.Id != null && _masterAnswerKey.TryGetValue(q.Id, out var savedAns))
				{
					q.Answer = savedAns;
					if (q.Id == "proj_location" && !string.IsNullOrWhiteSpace(savedAns))
					{
						ProjectLocation = savedAns;
					}
				}
				else if (q.Id == "proj_location" && !string.IsNullOrWhiteSpace(ProjectLocation))
				{
					q.Answer = ProjectLocation;
					_masterAnswerKey[q.Id] = ProjectLocation;
				}
				q.PropertyChanged += Question_PropertyChanged;
				Questions.Add(q);
			}
			EvaluateQuestionVisibility();
		}
		else if (step.Type == WizardStepType.Review)
		{
			RefreshAnswersList();
		}
	}

	private void Question_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(WizardQuestionViewModel.Answer) || e.PropertyName == nameof(WizardQuestionViewModel.BoolAnswer))
		{
			EvaluateQuestionVisibility();
			OnPropertyChanged(nameof(ProgressPercentage));

			if (sender is WizardQuestionViewModel q && !string.IsNullOrEmpty(q.Id))
			{
				_masterAnswerKey[q.Id] = q.Answer ?? "";
				if (q.Id == "proj_location")
				{
					ProjectLocation = q.Answer ?? "";
				}
				TriggerDebouncedSave();
			}
		}
	}

	private void TriggerDebouncedSave()
	{
		_saveDebounceCts?.Cancel();
		_saveDebounceCts = new System.Threading.CancellationTokenSource();
		var token = _saveDebounceCts.Token;

		Task.Run(async () =>
		{
			try
			{
				await Task.Delay(500, token);
				if (token.IsCancellationRequested) return;

				await _saveLock.WaitAsync(token);
				try
				{
					await InternalSaveDraftAsync(null, false, suppressAlert: true);
				}
				finally
				{
					_saveLock.Release();
				}
			}
			catch (TaskCanceledException) { /* Ignored */ }
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"[JobWizard] Debounced save failed: {ex.Message}");
			}
		});
	}

	private void EvaluateQuestionVisibility()
	{
		bool anyChanged = true;
		bool overallChanged = false;

		while (anyChanged)
		{
			anyChanged = false;
			foreach (var q in Questions)
			{
				bool newVisibility = true;
				if (string.IsNullOrEmpty(q.DependsOn))
				{
					newVisibility = true;
				}
				else
				{
					var parentQuestion = Questions.FirstOrDefault(p => p.Id == q.DependsOn);
					if (parentQuestion != null)
					{
						if (!parentQuestion.IsVisible)
						{
							newVisibility = false;
						}
						else if (string.IsNullOrEmpty(parentQuestion.Answer))
						{
							newVisibility = false;
						}
						else if (parentQuestion.IsMultiSelect)
						{
							var selectedOptions = parentQuestion.Answer.Split(',').Select(a => a.Trim()).ToList();
							var targetValues = q.DependsOnValue.Split('|').Select(v => v.Trim()).ToList();
							newVisibility = selectedOptions.Any(opt => targetValues.Contains(opt));
						}
						else
						{
							var targetValues = q.DependsOnValue.Split('|').Select(v => v.Trim()).ToList();
							newVisibility = targetValues.Any(v => parentQuestion.Answer.Contains(v, StringComparison.OrdinalIgnoreCase));
						}
					}
					else
					{
						newVisibility = false;
					}
				}

				if (q.IsVisible != newVisibility)
				{
					q.IsVisible = newVisibility;
					anyChanged = true;
					overallChanged = true;
				}
			}
		}

		if (overallChanged)
		{
			OnPropertyChanged(nameof(Questions));
		}
	}

	private async Task GenerateDynamicSteps()
	{
		if (_loadCategoriesTask != null && !_loadCategoriesTask.IsCompleted)
		{
			await _loadCategoriesTask;
		}

		_wizardSteps.Clear();
		System.Diagnostics.Debug.WriteLine($"[JobWizard] Generating Dynamic Steps. AllCategories Count: {_allCategories.Count}");

		if (_targetCategoryId != null)
		{
			// Edit Single Job Mode: Filter to specific category
			var targetCat = _allCategories.FirstOrDefault(c => c.Category.Id == _targetCategoryId);
			if (targetCat != null)
			{
				var catQuestions = ExtractQuestions(new List<SelectableCategoryViewModel> { targetCat });
				
				// Fetch the specific JobPost to get AdminQuestions from the JSON field
				var jobResult = await _apiClient.GetMyProjects.ExecuteAsync();
				var job = jobResult.Data?.MyProjects?.SelectMany(p => p.JobPosts).FirstOrDefault(j => j.Id == _targetJobPostId);

				if (!string.IsNullOrEmpty(job?.AdditionalQuestionsJson))
				{
					try
					{
						var extra = JsonNode.Parse(job.AdditionalQuestionsJson) as JsonArray;
						if (extra != null)
						{
							foreach (var qNode in extra)
							{
								var qId = qNode?["id"]?.GetValue<string>();
								var qText = qNode?["text"]?.GetValue<string>();
								var qType = qNode?["type"]?.GetValue<string>() ?? "text";
								var qReq = qNode?["required"]?.GetValue<bool>() ?? true;

								if (!string.IsNullOrEmpty(qId) && !string.IsNullOrEmpty(qText))
								{
									var qOptions = new List<string>();
									if (qNode?["options"] is JsonArray opts)
									{
										qOptions.AddRange(opts.Select(o => o?.GetValue<string>() ?? ""));
									}

									_questionTextCache[qId] = qText;
									catQuestions.Add(new WizardQuestionViewModel
									{
										Id = qId,
										Text = qText,
										Type = qType,
										CategoryName = "ADMIN CLARIFICATION",
										IsRequired = qReq,
										Options = qOptions,
										Answer = qType == "boolean" ? "False" : ""
									});
								}
							}
						}
					}
					catch { /* Ignore malformed JSON */ }
				}

				if (catQuestions.Any())
				{
					_wizardSteps.Add(new WizardStep
					{
						Type = WizardStepType.Questions,
						Title = _localizer?["JobWizard_CategoryQuestions", targetCat.Category.Name] ?? $"{targetCat.Category.Name} Questions",
						Questions = catQuestions
					});
				}
			}
			return;
		}

		// Normal Project Creation Flow
		_wizardSteps.Add(new WizardStep { Type = WizardStepType.CategorySelection, Title = _localizer?["JobWizard_SelectCategories"] ?? "Select Categories" });

		var globalCategories = _allCategories.Where(c => c.Category.IsGlobal).ToList();
		var selectedCategories = _allCategories.Where(c => !c.Category.IsGlobal && c.IsSelected).ToList();

		System.Diagnostics.Debug.WriteLine($"[JobWizard] Global Categories Found: {globalCategories.Count}");
		System.Diagnostics.Debug.WriteLine($"[JobWizard] Selected Categories Found: {selectedCategories.Count}");

		// 1. Global Questions Step
		var globalQuestions = ExtractQuestions(globalCategories);
		if (globalQuestions.Any())
		{
			System.Diagnostics.Debug.WriteLine($"[JobWizard] Adding General Questions Step with {globalQuestions.Count} questions.");
			_wizardSteps.Add(new WizardStep
			{
				Type = WizardStepType.Questions,
				Title = _localizer?["JobWizard_GeneralQuestions"] ?? "General Questions",
				Questions = globalQuestions
			});
		}
		else
		{
			System.Diagnostics.Debug.WriteLine("[JobWizard] NO Global questions extracted.");
		}

		// 2. Specific Category Steps
		foreach (var cat in selectedCategories)
		{
			var catQuestions = ExtractQuestions(new List<SelectableCategoryViewModel> { cat });
			if (catQuestions.Any())
			{
				_wizardSteps.Add(new WizardStep
				{
					Type = WizardStepType.Questions,
					Title = _localizer?["JobWizard_CategoryQuestions", cat.Category.Name] ?? $"{cat.Category.Name} Questions",
					Questions = catQuestions
				});
			}
		}

		// 3. Review Step
		if (!IsEditing)
		{
			_wizardSteps.Add(new WizardStep { Type = WizardStepType.Review, Title = _localizer?["JobWizard_ReviewSubmit"] ?? "Review & Submit" });
		}

		// 4. Project Details Step (Post-submission marketing/location questions)
		var projectDetailsCategory = _allCategories.FirstOrDefault(c => IsProjectDetailsCategory(c.Category.TemplateStructure));
		var projectDetailsQuestions = projectDetailsCategory != null
			? ExtractQuestions(new List<SelectableCategoryViewModel> { projectDetailsCategory })
			: new List<WizardQuestionViewModel>();

		_wizardSteps.Add(new WizardStep
		{
			Type = WizardStepType.Info,
			Title = _localizer?["JobWizard_InfoStepTitle"] ?? "Project Details",
			Questions = projectDetailsQuestions
		});
		
		System.Diagnostics.Debug.WriteLine($"[JobWizard] Rebuilt steps. Total steps: {_wizardSteps.Count}");
	}

	private string GetLocalizedValue(JsonNode? node, string lang, string fallbackLang = "bg")
	{
		if (node == null) return "";
		if (node is JsonObject obj)
		{
			return obj[lang]?.GetValue<string>() ?? obj[fallbackLang]?.GetValue<string>() ?? "";
		}
		return node.GetValue<string>() ?? "";
	}

	private List<string> GetLocalizedOptions(JsonNode? node, string lang, string fallbackLang = "bg")
	{
		var list = new List<string>();
		if (node == null) return list;

		if (node is JsonObject obj)
		{
			var array = obj[lang] as JsonArray ?? obj[fallbackLang] as JsonArray;
			if (array != null)
			{
				list.AddRange(array.Select(o => o?.GetValue<string>() ?? ""));
			}
		}
		else if (node is JsonArray array)
		{
			list.AddRange(array.Select(o => o?.GetValue<string>() ?? ""));
		}
		return list;
	}

	private List<WizardQuestionViewModel> ExtractQuestions(List<SelectableCategoryViewModel> categories)
	{
		var list = new List<WizardQuestionViewModel>();
		string currentLang = System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("bg") ? "bg" : "en";

		foreach (var cat in categories)
		{
			System.Diagnostics.Debug.WriteLine($"[JobWizard] Processing Category for questions: {cat.Category.Name}");

			if (!string.IsNullOrWhiteSpace(cat.Category.TemplateStructure))
			{
				try
				{
					var template = JsonNode.Parse(cat.Category.TemplateStructure);
					if (template == null)
					{
						System.Diagnostics.Debug.WriteLine($"[JobWizard] Template for {cat.Category.Name} parsed to NULL");
						continue;
					}

					if (template["questions"] is JsonArray qArray)
					{
						System.Diagnostics.Debug.WriteLine($"[JobWizard] Found {qArray.Count} questions in {cat.Category.Name}");
						foreach (var qNode in qArray)
						{
							if (qNode is JsonObject qObj)
							{
								var qType = qObj["type"]?.GetValue<string>() ?? "text";
								var qText = GetLocalizedValue(qObj["text"], currentLang);
								var qId = qObj["id"]?.GetValue<string>() ?? "";
								
								var qOptions = GetLocalizedOptions(qObj["options"], currentLang);

								if (!string.IsNullOrEmpty(qId)) _questionTextCache[qId] = qText;

								list.Add(new WizardQuestionViewModel
								{
									Id = qId,
									Text = qText,
									Type = qType,
									CategoryName = cat.Category.Name,
									IsRequired = qObj["required"]?.GetValue<bool>() ?? false,
									Options = qOptions,
									Answer = qType == "boolean" ? "False" : "",
									DependsOn = qObj["dependsOn"]?.GetValue<string>() ?? "",
									DependsOnValue = GetLocalizedValue(qObj["dependsOnValue"], currentLang)
								});
							}
						}
					}
					else
					{
						System.Diagnostics.Debug.WriteLine($"[JobWizard] 'questions' array NOT found in template for {cat.Category.Name}");
					}
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"[JobWizard] Error parsing template for {cat.Category.Name}: {ex.Message}");
				}
			}
			else
			{
				System.Diagnostics.Debug.WriteLine($"[JobWizard] TemplateStructure for {cat.Category.Name} is EMPTY or NULL.");
			}
		}

		System.Diagnostics.Debug.WriteLine($"[JobWizard] Total extracted questions: {list.Count}");
		return list;
	}

	private bool ValidateInfoStep()
	{
		TitleHasError = string.IsNullOrWhiteSpace(ProjectTitle);
		DescriptionHasError = string.IsNullOrWhiteSpace(ProjectDescription);
		LocationHasError = string.IsNullOrWhiteSpace(ProjectLocation);

		if (TitleHasError || DescriptionHasError || LocationHasError)
		{
			string title = _localizer?["JobWizard_Validation_Required"] ?? "Required";
			string msg = _localizer?["JobWizard_Validation_ProjectDetails"] ?? "Please enter a project title, description, and location.";
			string ok = _localizer?["JobWizard_OK"] ?? "OK";
			AppServiceLocator.Alerts.DisplayAlert(title, msg, ok);
			return false;
		}
		return true;
	}

	private bool ValidateCategoryStep()
	{
		var selectedCount = SelectableCategories.Count(c => c.IsSelected);
		if (selectedCount == 0)
		{
			CategorySelectionHasError = true;
			string title = _localizer?["JobWizard_Validation_Required"] ?? "Required";
			string msg = _localizer?["JobWizard_Validation_SelectCategory"] ?? "Please select at least one category.";
			string ok = _localizer?["JobWizard_OK"] ?? "OK";
			AppServiceLocator.Alerts.DisplayAlert(title, msg, ok);
			return false;
		}

		if (selectedCount > RemainingAiRequests)
		{
			CategorySelectionHasError = true;
			string errorTitle = _localizer?["JobWizard_AiLimitReached_Title"] ?? "Limit reached";
			string okText = _localizer?["JobWizard_OK"] ?? "OK";
			string errorMsg = string.Format(_localizer?["JobWizard_AiRequestsExceeded"] ?? "Please select fewer categories to fit within your remaining monthly AI limit of {0} requests. You've selected {1} categories. You can also contact support to upgrade your account.", RemainingAiRequests, selectedCount);
			AppServiceLocator.Alerts.DisplayAlert(errorTitle, errorMsg, okText);
			return false;
		}

		CategorySelectionHasError = false;
		return true;
	}

	private bool ValidateQuestionsStep()
	{
		if (Questions != null)
		{
			foreach (var q in Questions)
			{
				q.HasError = false;
			}
		}

		var missingQuestions = Questions.Where(q => q.IsVisible && q.IsRequired && string.IsNullOrWhiteSpace(q.Answer)).ToList();
		if (missingQuestions.Any())
		{
			foreach (var q in missingQuestions) q.HasError = true;
			string title = _localizer?["JobWizard_Validation_Required"] ?? "Required";
			string msg = _localizer?["JobWizard_Validation_RequiredQuestions"] ?? "Please answer all required questions marked with (*).";
			string ok = _localizer?["JobWizard_OK"] ?? "OK";
			AppServiceLocator.Alerts.DisplayAlert(title, msg, ok);
			return false;
		}
		return true;
	}

	private async Task HandleCategorySelectionChangedAsync(SelectableCategoryViewModel categoryVm)
	{
		if (_isUpdatingSelection) return;

		var selectedCount = SelectableCategories.Count(c => c.IsSelected);
		if (selectedCount > RemainingAiRequests)
		{
			string errorTitle = _localizer?["JobWizard_AiLimitReached_Title"] ?? "Limit reached";
			string okText = _localizer?["JobWizard_OK"] ?? "OK";
			string errorMsg = string.Format(_localizer?["JobWizard_AiRequestsExceeded"] ?? "Please select fewer categories to fit within your remaining monthly AI limit of {0} requests. You've selected {1} categories. You can also contact support to upgrade your account.", RemainingAiRequests, selectedCount);
			await AppServiceLocator.Alerts.DisplayAlert(errorTitle, errorMsg, okText);

			_isUpdatingSelection = true;
			categoryVm.IsSelected = !categoryVm.IsSelected;
			OnPropertyChanged(nameof(ProgressPercentage));
			_isUpdatingSelection = false;
			return;
		}

		try
		{
			IsBusy = true;
			bool success = await InternalSaveDraftAsync(specificCategory: null, projectOnly: false, suppressAlert: false);
			if (!success)
			{
				_isUpdatingSelection = true;
				categoryVm.IsSelected = !categoryVm.IsSelected;
				OnPropertyChanged(nameof(ProgressPercentage));
			}
		}
		catch (Exception ex)
		{
			await AppServiceLocator.Alerts.DisplayAlert("Error", ex.Message, "OK");
			_isUpdatingSelection = true;
			categoryVm.IsSelected = !categoryVm.IsSelected;
			OnPropertyChanged(nameof(ProgressPercentage));
		}
		finally
		{
			_isUpdatingSelection = false;
			IsBusy = false;
		}
	}

	public async Task<bool> SaveDraftAsync(SelectableCategoryViewModel? specificCategory = null, bool projectOnly = false)
	{
		if (IsBusy) return false;
		try
		{
			IsBusy = true;
			return await InternalSaveDraftAsync(specificCategory, projectOnly);
		}
		finally
		{
			IsBusy = false;
		}
	}

	private async Task<bool> InternalSaveDraftAsync(
		SelectableCategoryViewModel? specificCategory = null, 
		bool projectOnly = false, 
		bool suppressAlert = false)
	{
		var currentLang = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
		var prefix = currentLang.Equals("bg", StringComparison.OrdinalIgnoreCase) ? "Проект" : "Project";
		var fallbackTitle = $"{prefix} - {(currentLang.Equals("bg", StringComparison.OrdinalIgnoreCase) ? "Ремонт" : "Renovation")}";

		if (_currentProjectId == null)
		{
			var selectedCategories = SelectableCategories.Where(c => c.IsSelected).ToList();
			if (selectedCategories.Count == 0)
			{
				return true;
			}

			if (string.IsNullOrWhiteSpace(ProjectTitle))
			{
				ProjectTitle = selectedCategories.Count > 0 
					? $"{prefix} - {string.Join(" & ", selectedCategories.Select(c => GetLocalizedCategoryName(c.Category)))}"
					: fallbackTitle;
			}

			if (string.IsNullOrWhiteSpace(ProjectLocation))
			{
				if (_masterAnswerKey.TryGetValue("proj_location", out var savedLoc) && !string.IsNullOrWhiteSpace(savedLoc))
				{
					ProjectLocation = savedLoc;
				}
				else
				{
					ProjectLocation = "Sofia";
				}
			}

			if (string.IsNullOrWhiteSpace(ProjectDescription))
			{
				ProjectDescription = selectedCategories.Count > 0 
					? $"{(currentLang.Equals("bg", StringComparison.OrdinalIgnoreCase) ? "Ремонт за" : "Renovation project for")} {string.Join(", ", selectedCategories.Select(c => GetLocalizedCategoryName(c.Category)))}"
					: (currentLang.Equals("bg", StringComparison.OrdinalIgnoreCase) ? "Проект за ремонт" : "Renovation project");
			}

			var userResult = await _apiClient.GetCurrentUser.ExecuteAsync();
			if (userResult.Errors.Count > 0)
			{
				if (!suppressAlert)
				{
					string errorTitle = _localizer?["JobWizard_SubmissionError_Title"] ?? "Error";
					string okText = _localizer?["JobWizard_OK"] ?? "OK";
					await AppServiceLocator.Alerts.DisplayAlert(errorTitle, userResult.Errors[0].Message, okText);
				}
				return false;
			}
			if (userResult.Data?.CurrentUser == null)
			{
				if (!suppressAlert)
				{
					string errorTitle = _localizer?["JobWizard_SubmissionError_Title"] ?? "Error";
					string okText = _localizer?["JobWizard_OK"] ?? "OK";
					await AppServiceLocator.Alerts.DisplayAlert(errorTitle, "User not authenticated.", okText);
				}
				return false;
			}
			var userId = userResult.Data.CurrentUser.Id;

			var projectResult = await _apiClient.CreateProject.ExecuteAsync(Guid.Parse(userId), ProjectTitle, ProjectDescription, currentLang);
			if (projectResult.Errors.Count > 0 || projectResult.Data?.CreateProject == null)
			{
				if (!suppressAlert)
				{
					string errorTitle = _localizer?["JobWizard_SubmissionError_Title"] ?? "Error";
					string errorMsg = projectResult.Errors.Count > 0 ? projectResult.Errors[0].Message : "Failed to create project draft.";
					string okText = _localizer?["JobWizard_OK"] ?? "OK";
					await AppServiceLocator.Alerts.DisplayAlert(errorTitle, errorMsg, okText);
				}
				return false;
			}
			_currentProjectId = projectResult.Data.CreateProject.Id;
			_signalRService?.NotifyNotificationsStateChanged();
		}
		else
		{
			var selectedCategories = SelectableCategories.Where(c => c.IsSelected).ToList();
			if (ProjectTitle == "Renovation Project" || ProjectTitle.StartsWith("Project -") || ProjectTitle.StartsWith("Проект -") || ProjectTitle.StartsWith("Build -"))
			{
				ProjectTitle = selectedCategories.Count > 0 
					? $"{prefix} - {string.Join(" & ", selectedCategories.Select(c => GetLocalizedCategoryName(c.Category)))}"
					: fallbackTitle;
			}

			var updateResult = await _apiClient.UpdateProjectDetails.ExecuteAsync(_currentProjectId.Value, ProjectTitle, ProjectDescription, CurrentStep);
			if (updateResult.Errors.Count > 0)
			{
				if (!suppressAlert)
				{
					string errorTitle = _localizer?["JobWizard_SubmissionError_Title"] ?? "Error";
					string okText = _localizer?["JobWizard_OK"] ?? "OK";
					await AppServiceLocator.Alerts.DisplayAlert(errorTitle, updateResult.Errors[0].Message, okText);
				}
				return false;
			}
		}

		if (projectOnly) return true;

		// Delete any deselected categories from the project draft
		if (specificCategory == null)
		{
			var deselectedCategories = SelectableCategories.Where(c => !c.IsSelected).ToList();
			foreach (var cat in deselectedCategories)
			{
				if (_currentJobPostIds.TryGetValue(cat.Category.Id, out var jobId))
				{
					var deleteResult = await _apiClient.DeleteJobPost.ExecuteAsync(jobId);
					if (deleteResult.Errors.Count > 0)
					{
						if (!suppressAlert)
						{
							string errorTitle = _localizer?["JobWizard_SubmissionError_Title"] ?? "Error";
							string okText = _localizer?["JobWizard_OK"] ?? "OK";
							await AppServiceLocator.Alerts.DisplayAlert(errorTitle, deleteResult.Errors[0].Message, okText);
						}
						return false;
					}
					_currentJobPostIds.Remove(cat.Category.Id);
				}
			}
		}

		var selected = specificCategory != null 
			? new List<SelectableCategoryViewModel> { specificCategory }
			: SelectableCategories.Where(c => c.IsSelected).ToList();

		var answersJson = JsonSerializer.Serialize(_masterAnswerKey);

		foreach (var cat in selected)
		{
			if (!_currentJobPostIds.ContainsKey(cat.Category.Id))
			{
				var jobResult = await _apiClient.AddJobToProject.ExecuteAsync(
					_currentProjectId.Value,
					cat.Category.Id,
					cat.Category.Name,
					answersJson,
					ProjectLocation,
					null, "EUR", new List<string>(), PreferredSiteVisitDate
				);

				if (jobResult.Errors.Count > 0 || jobResult.Data?.AddJobToProject == null)
				{
					if (!suppressAlert)
					{
						string errorTitle = _localizer?["JobWizard_SubmissionError_Title"] ?? "Error";
						string errorMsg = jobResult.Errors.Count > 0 ? jobResult.Errors[0].Message : "Failed to add job to project.";
						string okText = _localizer?["JobWizard_OK"] ?? "OK";
						await AppServiceLocator.Alerts.DisplayAlert(errorTitle, errorMsg, okText);
					}
					return false;
				}
				_currentJobPostIds[cat.Category.Id] = jobResult.Data.AddJobToProject.Id;
			}
			else
			{
				var jobId = _currentJobPostIds[cat.Category.Id];
				var saveJobResult = await _apiClient.SaveJobPostDraft.ExecuteAsync(
					jobId,
					answersJson,
					cat.Category.Name,
					ProjectLocation,
					null, "EUR"
				);

				if (saveJobResult.Errors.Count > 0)
				{
					if (!suppressAlert)
					{
						string errorTitle = _localizer?["JobWizard_SubmissionError_Title"] ?? "Error";
						string okText = _localizer?["JobWizard_OK"] ?? "OK";
						await AppServiceLocator.Alerts.DisplayAlert(errorTitle, saveJobResult.Errors[0].Message, okText);
					}
					return false;
				}
			}
		}

		return true;
	}

	[RelayCommand]
	public async Task SubmitProject()
	{
		if (IsBusy) return;

		try
		{
			IsBusy = true;

			var selectedCategories = SelectableCategories.Where(c => c.IsSelected).ToList();
			if (string.IsNullOrWhiteSpace(ProjectTitle) || ProjectTitle == "Renovation Project" || ProjectTitle.StartsWith("Project -") || ProjectTitle.StartsWith("Проект -") || ProjectTitle.StartsWith("Build -"))
			{
				var currentLang = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
				var prefix = currentLang.Equals("bg", StringComparison.OrdinalIgnoreCase) ? "Проект" : "Project";
				var fallbackTitle = $"{prefix} - {(currentLang.Equals("bg", StringComparison.OrdinalIgnoreCase) ? "Ремонт" : "Renovation")}";

				ProjectTitle = selectedCategories.Count > 0 
					? $"{prefix} - {string.Join(" & ", selectedCategories.Select(c => GetLocalizedCategoryName(c.Category)))}"
					: fallbackTitle;
			}
			if (string.IsNullOrWhiteSpace(ProjectLocation))
			{
				if (_masterAnswerKey.TryGetValue("proj_location", out var savedLoc) && !string.IsNullOrWhiteSpace(savedLoc))
				{
					ProjectLocation = savedLoc;
				}
				else
				{
					ProjectLocation = "Sofia";
				}
			}
			if (string.IsNullOrWhiteSpace(ProjectDescription))
			{
				ProjectDescription = selectedCategories.Count > 0 
					? $"Renovation project for {string.Join(", ", selectedCategories.Select(c => c.Category.Name))}"
					: "Renovation project";
			}

			// Ensure everything is saved
			if (!await InternalSaveDraftAsync())
			{
				return;
			}

			if (_currentJobPostIds.Count == 0)
			{
				await AppServiceLocator.Alerts.DisplayAlert("Error", "No jobs to submit.", "OK");
				return;
			}

			// Trigger AI Generation immediately for all selected jobs
			var jobsToSubmit = new List<Guid>();
			foreach (var cat in selectedCategories)
			{
				if (_currentJobPostIds.TryGetValue(cat.Category.Id, out var jobId))
				{
					var answersHash = JsonSerializer.Serialize(_masterAnswerKey);
					if (!_lastSubmittedJobHashes.TryGetValue(jobId, out var lastHash) || lastHash != answersHash)
					{
						jobsToSubmit.Add(jobId);
					}
				}
			}

			if (jobsToSubmit.Count > RemainingAiRequests)
			{
				string errorTitle = _localizer?["JobWizard_AiLimitReached_Title"] ?? "Limit reached";
				string okText = _localizer?["JobWizard_OK"] ?? "OK";
				string errorMsg = string.Format(_localizer?["JobWizard_AiRequestsExceeded"] ?? "Please select fewer categories to fit within your remaining monthly AI limit of {0} requests. You've selected {1} categories. You can also contact support to upgrade your account.", RemainingAiRequests, jobsToSubmit.Count);
				await AppServiceLocator.Alerts.DisplayAlert(errorTitle, errorMsg, okText);
				return;
			}

			foreach (var jobId in jobsToSubmit)
			{
				var answersHash = JsonSerializer.Serialize(_masterAnswerKey);
				var submitResult = await _apiClient.SubmitJobForScopeGeneration.ExecuteAsync(jobId);
				if (submitResult.Errors.Count > 0)
				{
					string errorTitle = _localizer?["JobWizard_SubmissionError_Title"] ?? "Submission Error";
					string okText = _localizer?["JobWizard_OK"] ?? "OK";
					await AppServiceLocator.Alerts.DisplayAlert(errorTitle, submitResult.Errors[0].Message, okText);
					return;
				}
				_lastSubmittedJobHashes[jobId] = answersHash;
			}

			// Increment step to the Project Details Info step (Step N)
			if (CurrentStep < _wizardSteps.Count - 1)
			{
				CurrentStep++;
				LoadStepData(CurrentStep);
			}
		}
		catch (Exception ex)
		{
			await AppServiceLocator.Alerts.DisplayAlert("Error", ex.Message, "OK");
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	public async Task SaveProjectDetails()
	{
		if (IsBusy) return;

		try
		{
			IsBusy = true;

			// Validate questions on Project Details questionnaire step
			if (!ValidateQuestionsStep()) return;

			// Save questions to master key
			foreach (var q in Questions)
			{
				if (q.Id != null && !string.IsNullOrEmpty(q.Answer))
					_masterAnswerKey[q.Id] = q.Answer;
			}

			// Save draft (updates dynamic questions on the server)
			if (!await InternalSaveDraftAsync())
			{
				return;
			}

			IsOfferBuilding = true;
		}
		catch (Exception ex)
		{
			await AppServiceLocator.Alerts.DisplayAlert("Error", ex.Message, "OK");
		}
		finally
		{
			IsBusy = false;
		}
	}

	private static bool IsProjectDetailsCategory(string? templateStructure)
	{
		if (string.IsNullOrWhiteSpace(templateStructure)) return false;
		try
		{
			var node = System.Text.Json.Nodes.JsonNode.Parse(templateStructure);
			return node?["isProjectDetails"]?.GetValue<bool>() ?? false;
		}
		catch
		{
			return false;
		}
	}

	private string GetLocalizedCategoryName(IGetServiceCategories_ServiceCategories category)
	{
		var currentLang = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
		var translation = category.Translations?.FirstOrDefault(t => t.LanguageCode.Equals(currentLang, StringComparison.OrdinalIgnoreCase));
		return translation?.Name ?? category.Name;
	}

	public class WizardStep
	{
		public WizardStepType Type { get; set; }
		public string Title { get; set; } = string.Empty;
		public List<WizardQuestionViewModel> Questions { get; set; } = new();
	}

	public enum WizardStepType
	{
		Info,
		CategorySelection,
		Questions,
		Review
	}
}




