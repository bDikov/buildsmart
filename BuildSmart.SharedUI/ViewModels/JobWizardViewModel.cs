using BuildSmart.SharedUI.Services;
using BuildSmart.SharedUI.MauiMocks;
using BuildSmart.SharedUI.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BuildSmart.SharedUI.ViewModels;

public partial class JobWizardViewModel : ObservableObject, IQueryAttributable
{
	private readonly IBuildSmartApiClient _apiClient;

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
			if (stepType == WizardStepType.Review) return 100;
			
			if (stepType == WizardStepType.Info)
			{
				double mandatoryProgress = 0;
				mandatoryProgress += GetTextProgress(ProjectTitle, 15, 3.75);
				mandatoryProgress += GetTextProgress(ProjectLocation, 10, 3.75);
				mandatoryProgress += GetTextProgress(ProjectDescription, 40, 3.75);
				
				double infoProgress = mandatoryProgress;
				if (mandatoryProgress > 0 && PreferredSiteVisitDate.HasValue)
				{
					infoProgress += 3.75;
				}
				return infoProgress; // Max 15
			}
			
			if (stepType == WizardStepType.CategorySelection)
			{
				double baseCat = 15;
				if (SelectableCategories != null && SelectableCategories.Any(c => c.IsSelected)) return 30;
				return baseCat;
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
			
			double baseProgress = 30.0;
			if (_wizardSteps.Count == 1 || questionStartIdx == 0) 
			{
			    baseProgress = 0.0; // Single step edit mode
			}

			double remainingProgress = 100.0 - baseProgress;
			
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
					    // Checkboxes default to "False", so they should only count as progress if actually checked
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
				// If there are mandatory questions but NONE have been answered, don't count optional progress
				answeredQ = 0;
			}
			
			double stepFraction = totalQ > 0 ? answeredQ / totalQ : 1.0;
			
			int denominator = totalQuestionSteps + (reviewIdx != -1 ? 1 : 0);
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
	private Guid? _targetJobPostId;
	private Guid? _targetCategoryId;
	private Dictionary<Guid, Guid> _currentJobPostIds = new();

	// Legacy property for backward compatibility if needed, but we use _masterAnswerKey now
	public Dictionary<string, object> WizardAnswers { get; private set; } = new();

	private Task? _loadCategoriesTask;

	public JobWizardViewModel(IBuildSmartApiClient apiClient)
	{
		_apiClient = apiClient;
		InitializeSteps();
		_loadCategoriesTask = LoadCategoriesAsync();
	}

	private void InitializeSteps()
	{
		_wizardSteps.Clear();
		_wizardSteps.Add(new WizardStep { Type = WizardStepType.Info, Title = "Basic Info" });
		_wizardSteps.Add(new WizardStep { Type = WizardStepType.CategorySelection, Title = "Select Categories" });
		// Default placeholder
		_wizardSteps.Add(new WizardStep { Type = WizardStepType.Review, Title = "Review & Submit" });
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

					// Pre-fill answers
					if (firstJob != null && !string.IsNullOrEmpty(firstJob.JobDetails))
					{
						try
						{
							var flatAnswers = JsonSerializer.Deserialize<Dictionary<string, string>>(firstJob.JobDetails);
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

					// Position at the correct step
					if (_targetCategoryId != null)
					{
						CurrentStep = 0; // The only step in single-edit mode
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
					viewModel.PropertyChanged += (s, e) => 
					{
						if (e.PropertyName == nameof(SelectableCategoryViewModel.IsSelected))
						{
							OnPropertyChanged(nameof(ProgressPercentage));
						}
					};
					
					_allCategories.Add(viewModel);

					if (!cat.IsGlobal)
					{
						SelectableCategories.Add(viewModel);
					}
				}
				
				OnPropertyChanged(nameof(SelectableCategories));
				
				// Allow UI to update before proceeding
				await Task.Delay(100);
			}

			// Also check if user has projects to determine if we should show the swipe hint
			try
			{
				var projectsResult = await _apiClient.GetMyProjects.ExecuteAsync();
				if (projectsResult.Data?.MyProjects != null)
				{
					HasProjects = projectsResult.Data.MyProjects.Count > 0;
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
		if (currentStepType == WizardStepType.Info) return ValidateInfoStep();
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

			// 2. NAVIGATE IMMEDIATELY (UX Optimization)
			// Move to the next page so the user sees the new questions while we save in the background
			bool movedNext = false;
			if (CurrentStep < _wizardSteps.Count - 1)
			{
				CurrentStep++;
				LoadStepData(CurrentStep);
				movedNext = true;
			}

			// 3. BACKGROUND NETWORK CALLS
			if (currentStepType == WizardStepType.CategorySelection)
			{
				await InternalSaveDraftAsync(null, true);
			}
			else if (currentStepType == WizardStepType.Questions)
			{
				var stepTitle = _wizardSteps[currentStepIndex].Title;
				if (stepTitle == "General Questions")
				{
					await InternalSaveDraftAsync(null, true);
				}
				else
				{
					var categoryName = stepTitle.Replace(" Questions", "");
					var cat = SelectableCategories.FirstOrDefault(c => c.Category.Name == categoryName);
					if (cat != null)
					{
						await InternalSaveDraftAsync(cat);
						
						// Trigger AI Generation immediately for this category
						if (_currentJobPostIds.TryGetValue(cat.Category.Id, out var jobId))
						{
							var submitResult = await _apiClient.SubmitJobForScopeGeneration.ExecuteAsync(jobId);
							if (submitResult.Errors.Count > 0)
							{
								await AppServiceLocator.Alerts.DisplayAlert("Warning", $"Could not start AI for {categoryName}: {submitResult.Errors[0].Message}", "OK");
							}
						}
					}
					else
					{
						await InternalSaveDraftAsync();
					}
				}
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
			await InternalSaveDraftAsync();

			var jobsToRegenerate = _targetJobPostId != null
				? new List<Guid> { _targetJobPostId.Value }
				: _currentJobPostIds.Values.ToList();

			foreach (var jobId in jobsToRegenerate)
			{
				var result = await _apiClient.SubmitJobForScopeGeneration.ExecuteAsync(jobId);
				if (result.Errors.Count > 0)
				{
					await AppServiceLocator.Alerts.DisplayAlert("Error", result.Errors[0].Message, "OK");
					return;
				}
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
			CurrentStep--;
			LoadStepData(CurrentStep);
		}
	}

	private void LoadStepData(int stepIndex)
	{
		var step = _wizardSteps[stepIndex];

		// Always refresh questions if it's a question step
		if (step.Type == WizardStepType.Questions)
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
		}
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
		Console.WriteLine($"[JobWizard] Generating Dynamic Steps. AllCategories Count: {_allCategories.Count}");

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
						Title = $"{targetCat.Category.Name} Questions",
						Questions = catQuestions
					});
				}
			}
			return;
		}

		// Normal Project Creation Flow
		_wizardSteps.Add(new WizardStep { Type = WizardStepType.Info, Title = "Basic Info" });
		_wizardSteps.Add(new WizardStep { Type = WizardStepType.CategorySelection, Title = "Select Categories" });

		var globalCategories = _allCategories.Where(c => c.Category.IsGlobal).ToList();
		var selectedCategories = _allCategories.Where(c => !c.Category.IsGlobal && c.IsSelected).ToList();

		Console.WriteLine($"[JobWizard] Global Categories Found: {globalCategories.Count}");
		Console.WriteLine($"[JobWizard] Selected Categories Found: {selectedCategories.Count}");

		// 1. Global Questions Step
		var globalQuestions = ExtractQuestions(globalCategories);
		if (globalQuestions.Any())
		{
			Console.WriteLine($"[JobWizard] Adding General Questions Step with {globalQuestions.Count} questions.");
			_wizardSteps.Add(new WizardStep
			{
				Type = WizardStepType.Questions,
				Title = "General Questions",
				Questions = globalQuestions
			});
		}
		else
		{
			Console.WriteLine("[JobWizard] NO Global questions extracted.");
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
					Title = $"{cat.Category.Name} Questions",
					Questions = catQuestions
				});
			}
		}

		// 3. Review Step
		if (!IsEditing)
		{
			_wizardSteps.Add(new WizardStep { Type = WizardStepType.Review, Title = "Review & Submit" });
		}
		
		Console.WriteLine($"[JobWizard] Rebuilt steps. Total steps: {_wizardSteps.Count}");
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
			Console.WriteLine($"[JobWizard] Processing Category for questions: {cat.Category.Name}");

			if (!string.IsNullOrWhiteSpace(cat.Category.TemplateStructure))
			{
				try
				{
					var template = JsonNode.Parse(cat.Category.TemplateStructure);
					if (template == null)
					{
						Console.WriteLine($"[JobWizard] Template for {cat.Category.Name} parsed to NULL");
						continue;
					}

					if (template["questions"] is JsonArray qArray)
					{
						Console.WriteLine($"[JobWizard] Found {qArray.Count} questions in {cat.Category.Name}");
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
						Console.WriteLine($"[JobWizard] 'questions' array NOT found in template for {cat.Category.Name}");
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"[JobWizard] Error parsing template for {cat.Category.Name}: {ex.Message}");
				}
			}
			else
			{
				Console.WriteLine($"[JobWizard] TemplateStructure for {cat.Category.Name} is EMPTY or NULL.");
			}
		}

		Console.WriteLine($"[JobWizard] Total extracted questions: {list.Count}");
		return list;
	}

	private bool ValidateInfoStep()
	{
		TitleHasError = string.IsNullOrWhiteSpace(ProjectTitle);
		DescriptionHasError = string.IsNullOrWhiteSpace(ProjectDescription);
		LocationHasError = string.IsNullOrWhiteSpace(ProjectLocation);

		if (TitleHasError || DescriptionHasError || LocationHasError)
		{
			AppServiceLocator.Alerts.DisplayAlert("Required", "Please enter a project title, description, and location.", "OK");
			return false;
		}
		return true;
	}

	private bool ValidateCategoryStep()
	{
		if (!SelectableCategories.Any(c => c.IsSelected))
		{
			CategorySelectionHasError = true;
			AppServiceLocator.Alerts.DisplayAlert("Required", "Please select at least one category.", "OK");
			return false;
		}
		CategorySelectionHasError = false;
		return true;
	}

	private bool ValidateQuestionsStep()
	{
		var missingQuestions = Questions.Where(q => q.IsVisible && q.IsRequired && string.IsNullOrWhiteSpace(q.Answer)).ToList();
		if (missingQuestions.Any())
		{
			foreach (var q in missingQuestions) q.HasError = true;
			AppServiceLocator.Alerts.DisplayAlert("Required", "Please answer all required questions marked with (*).", "OK");
			return false;
		}
		return true;
	}

	public async Task SaveDraftAsync(SelectableCategoryViewModel? specificCategory = null, bool projectOnly = false)
	{
		if (IsBusy) return;
		try
		{
			IsBusy = true;
			await InternalSaveDraftAsync(specificCategory, projectOnly);
		}
		finally
		{
			IsBusy = false;
		}
	}

	private async Task InternalSaveDraftAsync(SelectableCategoryViewModel? specificCategory = null, bool projectOnly = false)
	{
		if (_currentProjectId == null)
		{
			var userResult = await _apiClient.GetCurrentUser.ExecuteAsync();
			if (userResult.Errors.Count > 0 || userResult.Data?.CurrentUser == null) return;
			var userId = userResult.Data.CurrentUser.Id;

			var currentLang = System.Globalization.CultureInfo.CurrentUICulture.Name;
			var projectResult = await _apiClient.CreateProject.ExecuteAsync(Guid.Parse(userId), ProjectTitle, ProjectDescription, currentLang);
			if (projectResult.Errors.Count > 0)
			{
				await AppServiceLocator.Alerts.DisplayAlert("Error", "Failed to create project draft.", "OK");
				return;
			}
			_currentProjectId = projectResult.Data.CreateProject.Id;
		}

		if (projectOnly) return;

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
					null, "USD", new List<string>(), PreferredSiteVisitDate
				);

				if (jobResult.Errors.Count > 0)
				{
					await AppServiceLocator.Alerts.DisplayAlert("Error", $"AddJob Error: {jobResult.Errors[0].Message}", "OK");
				}
				else if (jobResult.Data?.AddJobToProject != null)
				{
					_currentJobPostIds[cat.Category.Id] = jobResult.Data.AddJobToProject.Id;
				}
			}
			else
			{
				var jobId = _currentJobPostIds[cat.Category.Id];
				await _apiClient.SaveJobPostDraft.ExecuteAsync(
					jobId,
					answersJson,
					ProjectDescription,
					ProjectLocation,
					null, "USD"
				);
			}
		}
	}

	[RelayCommand]
	public async Task SubmitProject()
	{
		if (IsBusy) return;

		try
		{
			IsBusy = true;
			// Ensure everything is saved
			await InternalSaveDraftAsync();

			if (_currentJobPostIds.Count == 0)
			{
				await AppServiceLocator.Alerts.DisplayAlert("Error", "No jobs to submit.", "OK");
				return;
			}

			// Jobs are now individually submitted to the AI when the user clicks 'Next' on their respective question pages.
			// No need to batch submit them here again.

			await AppServiceLocator.Alerts.DisplayAlert("Success", "Project submitted! AI is generating your scopes.", "OK");
			await AppServiceLocator.Navigation.NavigateToAsync("//BlazorHostPage");
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




