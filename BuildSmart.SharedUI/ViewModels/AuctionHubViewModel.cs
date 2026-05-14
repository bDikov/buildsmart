using BuildSmart.SharedUI.Services;
using BuildSmart.SharedUI.MauiMocks;
using BuildSmart.SharedUI.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BuildSmart.SharedUI.ViewModels;

[QueryProperty(nameof(JobId), "jobId")]
public partial class AuctionHubViewModel : ObservableObject
{
	private readonly IBuildSmartApiClient _apiClient;
	private readonly Services.SignalRService _signalRService;

	public AuctionHubViewModel(IBuildSmartApiClient apiClient, Services.SignalRService signalRService)
	{
		_apiClient = apiClient;
		_signalRService = signalRService;
	}

	public async Task InitializeAsync()
	{
		_signalRService.QuestionUpdated += OnQuestionUpdated;
		_signalRService.NewReplyReceived += OnNewReplyReceived;

		if (!string.IsNullOrEmpty(JobId))
		{
			await _signalRService.ConnectAsync();
			await _signalRService.JoinAuctionGroupAsync(JobId);

			if (Guid.TryParse(JobId, out var id))
			{
				await LoadAuctionAsync(id);
			}
		}
	}

	public async Task CleanupAsync()
	{
		_signalRService.QuestionUpdated -= OnQuestionUpdated;
		_signalRService.NewReplyReceived -= OnNewReplyReceived;

		if (!string.IsNullOrEmpty(JobId))
		{
			await _signalRService.LeaveAuctionGroupAsync(JobId);
		}
	}

	private void OnQuestionUpdated(System.Text.Json.JsonElement payload)
	{
		if (Guid.TryParse(JobId, out var id))
		{
			_ = LoadAuctionAsync(id);
		}
	}

	private void OnNewReplyReceived(System.Text.Json.JsonElement payload)
	{
		if (Guid.TryParse(JobId, out var id))
		{
			_ = LoadAuctionAsync(id);
		}
	}

	[ObservableProperty]
	private string? _jobId;

	[ObservableProperty]
	private IGetAuctionById_AuctionById? _auction;

	[ObservableProperty]
	private IReadOnlyList<IGetJobTasks_AllJobPosts_JobTasks>? _jobTasks;

	[ObservableProperty]
	private IGetAuctionById_AuctionById_Bids? _myBid;

	[ObservableProperty]
	private bool _hasSubmittedBid;

	public ObservableCollection<QuestionViewModel> Questions { get; } = new();

	[ObservableProperty]
	private Guid? _currentTradesmanProfileId;

	[ObservableProperty]
	private bool _isBusy;

	partial void OnJobIdChanged(string? value)
	{
		if (Guid.TryParse(value, out var id))
		{
			LoadAuctionAsync(id);
		}
	}

	private async Task LoadAuctionAsync(Guid jobId)
	{
	    if (IsBusy) return;

	    try
	    {
	        IsBusy = true;
			// Fetch current user's profile ID to control Edit button visibility
			var userResult = await _apiClient.GetCurrentUser.ExecuteAsync();
			CurrentTradesmanProfileId = userResult.Data?.CurrentUser?.TradesmanProfile?.Id != null ? Guid.Parse(userResult.Data.CurrentUser.TradesmanProfile.Id) : null;

			var result = await _apiClient.GetAuctionById.ExecuteAsync(jobId);
			var tasksResult = await _apiClient.GetJobTasks.ExecuteAsync(jobId);

			if (result.Errors.Count == 0 && result.Data?.AuctionById != null)
			{
				Auction = result.Data.AuctionById;

				if (CurrentTradesmanProfileId.HasValue && Auction.Bids != null)
				{
					var profileIdStr = CurrentTradesmanProfileId.Value.ToString();
					MyBid = Auction.Bids.FirstOrDefault(b => b.TradesmanProfile?.Id != null && Guid.Parse(b.TradesmanProfile.Id) == CurrentTradesmanProfileId.Value);
					HasSubmittedBid = MyBid != null;
				}
				else
				{
					MyBid = null;
					HasSubmittedBid = false;
				}

				if (tasksResult.Errors.Count == 0 && tasksResult.Data?.AllJobPosts != null && tasksResult.Data.AllJobPosts.Count > 0)
				{
					JobTasks = tasksResult.Data.AllJobPosts[0].JobTasks;
				}
				else
				{
					JobTasks = null;
				}

				Questions.Clear();
				if (Auction.Questions != null)
				{
					foreach (var q in Auction.Questions)
					{
						Questions.Add(new QuestionViewModel(q, LoadMoreRepliesAsync));
					}
				}
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
	private async Task LoadMoreRepliesAsync(QuestionViewModel questionVm)
	{
		if (questionVm == null || IsBusy) return;

		try
		{
			IsBusy = true;
			var result = await _apiClient.GetQuestionReplies.ExecuteAsync(
				questionVm.Question.Id,
				questionVm.Replies.Count,
				5);

			if (result.Errors.Count > 0)
			{
				await AppServiceLocator.Alerts.DisplayAlert("Error", result.Errors[0].Message, "OK");
				return;
			}

			if (result.Data?.QuestionReplies?.Replies != null)
			{
				questionVm.AddReplies(result.Data.QuestionReplies.Replies);
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
	private async Task ToggleConversationAsync(QuestionViewModel questionVm)
	{
		if (questionVm == null) return;
		await questionVm.ToggleConversationCommand.ExecuteAsync(null);
	}

	[RelayCommand]
	private async Task BackAsync()
	{
		await AppServiceLocator.Navigation.NavigateToAsync("..");
	}

	[RelayCommand]
	private async Task GoToPlaceBidAsync()
	{
		if (string.IsNullOrEmpty(JobId))
		{
			await AppServiceLocator.Alerts.DisplayAlert("Error", "No job selected.", "OK");
			return;
		}

		await AppServiceLocator.Navigation.NavigateToAsync($"{"PlaceBidPage"}?jobId={JobId}");
	}

	[RelayCommand]
	private async Task EditQuestionAsync(IQuestionDetails question)
	{
		if (question == null) return;

		string newText = await AppServiceLocator.Alerts.DisplayPromptAsync("Edit Question", "Update your public question:", "Save", "Cancel", initialValue: question.QuestionText);
		if (string.IsNullOrWhiteSpace(newText) || newText == question.QuestionText) return;

		try
		{
			IsBusy = true;
			var result = await _apiClient.EditJobQuestion.ExecuteAsync(question.Id, newText);

			if (result.Errors.Count > 0)
			{
				await AppServiceLocator.Alerts.DisplayAlert("Error", result.Errors[0].Message, "OK");
				return;
			}

			var updatedQuestion = result.Data?.EditJobQuestion;
			if (updatedQuestion != null)
			{
				var vm = Questions.FirstOrDefault(q => q.Question?.Id == question.Id);
				if (vm != null)
				{
					vm.UpdateQuestion(updatedQuestion);
				}
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
	private async Task EditNestedQuestionAsync(IQuestionReplyDetails reply)
	{
		if (reply == null) return;

		string newText = await AppServiceLocator.Alerts.DisplayPromptAsync("Edit Reply", "Update your reply:", "Save", "Cancel", initialValue: reply.QuestionText);
		if (string.IsNullOrWhiteSpace(newText) || newText == reply.QuestionText) return;

		try
		{
			IsBusy = true;
			var result = await _apiClient.EditJobQuestion.ExecuteAsync(reply.Id, newText);

			if (result.Errors.Count > 0)
			{
				await AppServiceLocator.Alerts.DisplayAlert("Error", result.Errors[0].Message, "OK");
				return;
			}

			if (Guid.TryParse(JobId, out var id))
			{
				await LoadAuctionAsync(id);
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
	private async Task EditAnswerAsync(IQuestionDetails question)
	{
		if (question == null) return;

		string newAnswer = await AppServiceLocator.Alerts.DisplayPromptAsync("Edit Answer", "Update your answer:", "Save", "Cancel", initialValue: question.AnswerText);
		if (string.IsNullOrWhiteSpace(newAnswer) || newAnswer == question.AnswerText) return;

		try
		{
			IsBusy = true;
			var result = await _apiClient.EditJobAnswer.ExecuteAsync(question.Id, newAnswer);

			if (result.Errors.Count > 0)
			{
				await AppServiceLocator.Alerts.DisplayAlert("Error", result.Errors[0].Message, "OK");
				return;
			}

			var updatedQuestion = result.Data?.EditJobAnswer;
			if (updatedQuestion != null)
			{
				var vm = Questions.FirstOrDefault(q => q.Question?.Id == question.Id);
				if (vm != null)
				{
					vm.UpdateAnswer(updatedQuestion);
				}
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
	private async Task AskQuestionAsync()
	{
		if (Auction == null) return;

		string questionText = await AppServiceLocator.Alerts.DisplayPromptAsync("Ask Homeowner", "Your question will be public:", "Ask", "Cancel", "Type your question...");
		if (string.IsNullOrWhiteSpace(questionText)) return;

		try
		{
			IsBusy = true;

			// Get Current User Profile Id
			var userResult = await _apiClient.GetCurrentUser.ExecuteAsync();
			var profileId = userResult.Data?.CurrentUser?.TradesmanProfile?.Id;

			if (profileId == null)
			{
				await AppServiceLocator.Alerts.DisplayAlert("Error", "Tradesman profile not found.", "OK");
				return;
			}

			var result = await _apiClient.AskJobQuestion.ExecuteAsync(Guid.Parse(profileId), Auction.Job.Id, questionText);

			if (result.Errors.Count > 0)
			{
				await AppServiceLocator.Alerts.DisplayAlert("Error", result.Errors[0].Message, "OK");
				return;
			}

			if (Guid.TryParse(JobId, out var id))
			{
				await LoadAuctionAsync(id);
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
	private async Task ReplyToQuestionAsync(IQuestionDetails question)
	{
		if (question == null) return;
		await ExecuteReplyAsync(question.Id);
	}

	[RelayCommand]
	private async Task ReplyToNestedQuestionAsync(IQuestionReplyDetails reply)
	{
		if (reply == null) return;
		await ExecuteReplyAsync(reply.ParentQuestionId ?? reply.Id);
	}

	private async Task ExecuteReplyAsync(Guid parentQuestionId)
	{
		string replyText = await AppServiceLocator.Alerts.DisplayPromptAsync("Reply", "Type your reply:", "Send", "Cancel", "...");
		if (string.IsNullOrWhiteSpace(replyText)) return;

		try
		{
			IsBusy = true;

			var result = await _apiClient.ReplyToJobQuestion.ExecuteAsync(parentQuestionId, replyText);

			if (result.Errors.Count > 0)
			{
				await AppServiceLocator.Alerts.DisplayAlert("Error", result.Errors[0].Message, "OK");
				return;
			}

			var newReply = result.Data?.ReplyToJobQuestion;
			if (newReply != null)
			{
				var vm = Questions.FirstOrDefault(q => q.Question?.Id == parentQuestionId);
				if (vm != null)
				{
					vm.AddReply(newReply);
				}
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
}





