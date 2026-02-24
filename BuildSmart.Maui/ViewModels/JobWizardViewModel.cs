using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BuildSmart.Maui.ViewModels;

public partial class JobWizardViewModel : ObservableObject, IQueryAttributable
{
	private readonly IBuildSmartApiClient _apiClient;

	// --- Steps & Visibility ---
	private List<WizardStep> _wizardSteps = new();

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsInfoStepVisible))]
	[NotifyPropertyChangedFor(nameof(IsCategoryStepVisible))]
	[NotifyPropertyChangedFor(nameof(IsQuestionStepVisible))]
	[NotifyPropertyChangedFor(nameof(IsReviewStepVisible))]
	[NotifyPropertyChangedFor(nameof(CurrentStepTitle))]
    [NotifyPropertyChangedFor(nameof(NextButtonText))]
	private int _currentStep = 0;

	public bool IsInfoStepVisible => _wizardSteps.Any() && CurrentStep < _wizardSteps.Count && _wizardSteps[CurrentStep].Type == WizardStepType.Info;
	public bool IsCategoryStepVisible => _wizardSteps.Any() && CurrentStep < _wizardSteps.Count && _wizardSteps[CurrentStep].Type == WizardStepType.CategorySelection;
	public bool IsQuestionStepVisible => _wizardSteps.Any() && CurrentStep < _wizardSteps.Count && _wizardSteps[CurrentStep].Type == WizardStepType.Questions;
	public bool IsReviewStepVisible => _wizardSteps.Any() && CurrentStep < _wizardSteps.Count && _wizardSteps[CurrentStep].Type == WizardStepType.Review;

	public string CurrentStepTitle => _wizardSteps.Any() && CurrentStep < _wizardSteps.Count ? _wizardSteps[CurrentStep].Title : "";

    public string NextButtonText => (IsEditing && CurrentStep == _wizardSteps.Count - 1) ? "Save & Re-generate" : "Next";

	// --- Data ---
	[ObservableProperty]
	private ObservableCollection<SelectableCategoryViewModel> _selectableCategories = new();

	private List<SelectableCategoryViewModel> _allCategories = new();

	[ObservableProperty]
	private string _projectTitle = string.Empty;

	[ObservableProperty]
	private string _projectDescription = string.Empty;

	[ObservableProperty]
	private string _projectLocation = string.Empty;

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
	private bool _isBusy;

	[ObservableProperty]
	private bool _isEditing;

    public ObservableCollection<KeyValuePair<string, string>> AnswersList { get; } = new();

    private void RefreshAnswersList()
    {
        AnswersList.Clear();
        foreach (var kvp in _masterAnswerKey)
        {
            var text = _questionTextCache.TryGetValue(kvp.Key, out var qText) ? qText : kvp.Key;
            AnswersList.Add(new KeyValuePair<string, string>(text, kvp.Value));
        }
    }

	private Guid? _currentProjectId;
	private Guid? _targetJobPostId;
	private Guid? _targetCategoryId;
	private Dictionary<Guid, Guid> _currentJobPostIds = new();

	// Legacy property for backward compatibility if needed, but we use _masterAnswerKey now
	public Dictionary<string, object> WizardAnswers { get; private set; } = new();

	public JobWizardViewModel(IBuildSmartApiClient apiClient)
	{
		_apiClient = apiClient;
		InitializeSteps();
		LoadCategoriesAsync();
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
		if (query.TryGetValue("JobPostId", out var jpid) && Guid.TryParse(jpid.ToString(), out var jobId))
			_targetJobPostId = jobId;

		if (query.TryGetValue("TargetCategoryId", out var tcid) && Guid.TryParse(tcid.ToString(), out var catId))
			_targetCategoryId = catId;

		if (query.ContainsKey("ProjectId"))
		{
			if (Guid.TryParse(query["ProjectId"].ToString(), out var projectId))
			{
				_currentProjectId = projectId;
				IsEditing = true;
				MainThread.BeginInvokeOnMainThread(async () => await LoadExistingProjectAsync(projectId));
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
						_currentJobPostIds[job.ServiceCategory.Id] = job.Id;
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
									_masterAnswerKey[kvp.Key] = kvp.Value;
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
			await Shell.Current.DisplayAlert("Error", $"Failed to load draft: {ex.Message}", "OK");
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
				await Shell.Current.DisplayAlert("GraphQL Error", errorMessages, "OK");
			}
			else if (result.Data?.ServiceCategories != null)
			{
				SelectableCategories.Clear();
				_allCategories.Clear();

				foreach (var cat in result.Data.ServiceCategories)
				{
					var viewModel = new SelectableCategoryViewModel(cat);
					_allCategories.Add(viewModel);

					if (!cat.IsGlobal)
					{
						SelectableCategories.Add(viewModel);
					}
				}
			}
		}
		catch (Exception ex)
		{
			await Shell.Current.DisplayAlert("Error", $"Failed to load categories: {ex.Message}", "OK");
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	public async Task GoToNextStep()
	{
		if (CurrentStep >= _wizardSteps.Count) return;

		var currentStepType = _wizardSteps[CurrentStep].Type;

		if (currentStepType == WizardStepType.Info)
		{
			if (!ValidateInfoStep()) return;
		}
		else if (currentStepType == WizardStepType.CategorySelection)
		{
			if (!ValidateCategoryStep()) return;

			await GenerateDynamicSteps();
			await SaveDraftAsync();
		}
		else if (currentStepType == WizardStepType.Questions)
		{
			if (!ValidateQuestionsStep()) return;

			// Save current questions to master key
			foreach (var q in Questions)
			{
				if (!string.IsNullOrEmpty(q.Answer))
					_masterAnswerKey[q.Id] = q.Answer;
			}

			await SaveDraftAsync();
		}

		if (CurrentStep < _wizardSteps.Count - 1)
		{
			CurrentStep++;
			LoadStepData(CurrentStep);
		}
		else if (IsEditing)
		{
			// We are on the last question step and in Edit mode.
			await SaveAndRegenerateAsync();
		}
	}

	private async Task SaveAndRegenerateAsync()
	{
		if (IsBusy) return;
		try 
		{
			IsBusy = true;
			await SaveDraftAsync();
			
			var jobsToRegenerate = _targetJobPostId != null 
				? new List<Guid> { _targetJobPostId.Value }
				: _currentJobPostIds.Values.ToList();

			foreach (var jobId in jobsToRegenerate)
			{
				var result = await _apiClient.SubmitJobForScopeGeneration.ExecuteAsync(jobId);
				if (result.Errors.Count > 0)
				{
					await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
					return;
				}
			}
			
			await Shell.Current.DisplayAlert("Success", "Answers updated. AI is re-generating your scope.", "OK");
			await Shell.Current.GoToAsync(".."); // Go back to Project Details
		}
		catch (Exception ex)
		{
			await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
		}
		finally
		{
			IsBusy = false;
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
			Questions.Clear();
			foreach (var q in step.Questions)
			{
				if (_masterAnswerKey.TryGetValue(q.Id, out var savedAns))
				{
					q.Answer = savedAns;
				}
				Questions.Add(q);
			}
		}
        else if (step.Type == WizardStepType.Review)
        {
            RefreshAnswersList();
        }
	}

	private async Task GenerateDynamicSteps()
	{
		_wizardSteps.Clear();

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
		var selectedCategories = SelectableCategories.Where(c => c.IsSelected).ToList();

		// 1. Global Questions Step
		var globalQuestions = ExtractQuestions(globalCategories);
		if (globalQuestions.Any())
		{
			_wizardSteps.Add(new WizardStep
			{
				Type = WizardStepType.Questions,
				Title = "General Questions",
				Questions = globalQuestions
			});
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
	}

		private List<WizardQuestionViewModel> ExtractQuestions(List<SelectableCategoryViewModel> categories)

		{

			var list = new List<WizardQuestionViewModel>();

			foreach (var cat in categories)

			{

	            System.Diagnostics.Debug.WriteLine($"Processing Category: {cat.Category.Name}");

	            System.Diagnostics.Debug.WriteLine($"TemplateStructure: {cat.Category.TemplateStructure}");

	

				if (!string.IsNullOrWhiteSpace(cat.Category.TemplateStructure))

				{

					try

					{

						var template = JsonNode.Parse(cat.Category.TemplateStructure);

	                    if (template == null)

	                    {

	                        System.Diagnostics.Debug.WriteLine("Template parsed to NULL");

	                        continue;

	                    }

	

						if (template["questions"] is JsonArray qArray)

						{

	                        System.Diagnostics.Debug.WriteLine($"Found {qArray.Count} questions in JSON array.");

							foreach (var qNode in qArray)

							{

																if (qNode is JsonObject qObj)

								

																{

								

									                                var qType = qObj["type"]?.GetValue<string>() ?? "text";

								                                    var qText = qObj["text"]?.GetValue<string>() ?? "";

								                                    var qId = qObj["id"]?.GetValue<string>() ?? "";

								

									                                var qOptions = new List<string>();

								

									                                if (qObj["options"] is JsonArray opts)

								

									                                {

								

									                                    qOptions.AddRange(opts.Select(o => o?.GetValue<string>() ?? ""));

								

									                                }

								

									                                if (!string.IsNullOrEmpty(qId)) _questionTextCache[qId] = qText;

								

																	list.Add(new WizardQuestionViewModel

								

																	{

								

																		Id = qId,

								

																		Text = qText,

								

																		Type = qType,

								

																		CategoryName = cat.Category.Name,

								

																		IsRequired = qObj["required"]?.GetValue<bool>() ?? false,

								

									                                    Options = qOptions,

								

								                                        Answer = qType == "boolean" ? "False" : ""

								

																	});

								

																}

							}

						}

	                    else

	                    {

	                        System.Diagnostics.Debug.WriteLine("'questions' array NOT found in template.");

	                    }

					}

					catch (Exception ex)

	                {

	                    System.Diagnostics.Debug.WriteLine($"Error parsing template: {ex}");

	                }

				}

	            else

	            {

	                 System.Diagnostics.Debug.WriteLine("TemplateStructure is EMPTY or NULL.");

	            }

			}

	        System.Diagnostics.Debug.WriteLine($"Total extracted questions: {list.Count}");

			return list;

		}

	private bool ValidateInfoStep()
	{
		TitleHasError = string.IsNullOrWhiteSpace(ProjectTitle);
		DescriptionHasError = string.IsNullOrWhiteSpace(ProjectDescription);
		LocationHasError = string.IsNullOrWhiteSpace(ProjectLocation);

		if (TitleHasError || DescriptionHasError || LocationHasError)
		{
			Shell.Current.DisplayAlert("Required", "Please enter a project title, description, and location.", "OK");
			return false;
		}
		return true;
	}

	private bool ValidateCategoryStep()
	{
		if (!_selectableCategories.Any(c => c.IsSelected))
		{
			CategorySelectionHasError = true;
			Shell.Current.DisplayAlert("Required", "Please select at least one category.", "OK");
			return false;
		}
		CategorySelectionHasError = false;
		return true;
	}

	private bool ValidateQuestionsStep()
	{
		var missingQuestions = Questions.Where(q => q.IsRequired && string.IsNullOrWhiteSpace(q.Answer)).ToList();
		if (missingQuestions.Any())
		{
			foreach (var q in missingQuestions) q.HasError = true;
			Shell.Current.DisplayAlert("Required", "Please answer all required questions marked with (*).", "OK");
			return false;
		}
		return true;
	}

	public async Task SaveDraftAsync()
	{
		if (IsBusy) return;
		try
		{
			IsBusy = true;

			if (_currentProjectId == null)
			{
				var userResult = await _apiClient.GetCurrentUser.ExecuteAsync();
				if (userResult.Errors.Count > 0 || userResult.Data?.CurrentUser == null) return;
				var userId = userResult.Data.CurrentUser.Id;

				var projectResult = await _apiClient.CreateProject.ExecuteAsync(userId, ProjectTitle, ProjectDescription);
				if (projectResult.Errors.Count > 0)
				{
					await Shell.Current.DisplayAlert("Error", "Failed to create project draft.", "OK");
					return;
				}
				_currentProjectId = projectResult.Data.CreateProject.Id;
			}

			var selected = SelectableCategories.Where(c => c.IsSelected).ToList();

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
						null, "USD", new List<string>()
					);

					if (jobResult.Data?.AddJobToProject != null)
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

					// If we are in single edit mode, the answers are already part of the JSON 
                    // in SaveJobPostDraft call above. No separate mutation is needed.
				}
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Auto-save failed: {ex.Message}");
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	public async Task SubmitProject()
	{
		if (IsBusy) return;

		// Ensure everything is saved
		await SaveDraftAsync();

		if (_currentJobPostIds.Count == 0)
		{
			await Shell.Current.DisplayAlert("Error", "No jobs to submit.", "OK");
			return;
		}

		try
		{
			IsBusy = true;
			foreach (var jobId in _currentJobPostIds.Values)
			{
				var result = await _apiClient.SubmitJobPost.ExecuteAsync(jobId);
				if (result.Errors.Count > 0)
				{
					var msg = string.Join("\n", result.Errors.Select(e => e.Message));
					await Shell.Current.DisplayAlert("Submission Failed", msg, "OK");
					return;
				}
			}

			await Shell.Current.DisplayAlert("Success", "Project submitted for review!", "OK");
			await Shell.Current.GoToAsync("//MyProjectsPage");
		}
		catch (Exception ex)
		{
			await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
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