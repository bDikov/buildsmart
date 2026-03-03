using BuildSmart.Maui.ViewModels;
using BuildSmart.Maui.GraphQL;

namespace BuildSmart.Maui.Views;

public partial class ProjectDetailPage : ContentPage
{
    private readonly ProjectDetailViewModel _viewModel;

    public ProjectDetailPage(ProjectDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    private async void OnEditAnswersClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is IJobPostDetails job)
        {
            await _viewModel.EditAnswersCommand.ExecuteAsync(job);
        }
    }

    private async void OnReviewScopeClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is IJobPostDetails job)
        {
            await _viewModel.ReviewScopeCommand.ExecuteAsync(job);
        }
    }

    private async void OnReplyToAdminClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is IJobPostDetails job)
        {
            await _viewModel.RespondToAdminCommand.ExecuteAsync(job);
        }
    }

    private async void OnEditFeedbackClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is IFeedbackDetails feedback)
        {
            await _viewModel.EditFeedbackCommand.ExecuteAsync(feedback);
        }
    }

    private async void OnEditFeedbackReplyClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is IFeedbackReplyDetails reply)
        {
            await _viewModel.EditFeedbackReplyCommand.ExecuteAsync(reply);
        }
    }

    private async void OnEditNestedQuestionClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is IQuestionReplyDetails reply)
        {
            await _viewModel.EditNestedQuestionCommand.ExecuteAsync(reply);
        }
    }

    private async void OnReplyFeedbackClicked(object sender, TappedEventArgs e)
    {
        if (e.Parameter is IFeedbackDetails feedback)
        {
            await _viewModel.ReplyToFeedbackCommand.ExecuteAsync(feedback);
        }
        else if (e.Parameter is IFeedbackReplyDetails reply)
        {
            await _viewModel.ReplyToNestedFeedbackCommand.ExecuteAsync(reply);
        }
    }

    private async void OnReplyQuestionClicked(object sender, TappedEventArgs e)
    {
        if (e.Parameter is IQuestionDetails question)
        {
            await _viewModel.ReplyToQuestionCommand.ExecuteAsync(question);
        }
        else if (e.Parameter is IQuestionReplyDetails reply)
        {
            await _viewModel.ReplyToNestedQuestionCommand.ExecuteAsync(reply);
        }
    }

    private async void OnAnswerQuestionClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is IQuestionDetails question)
        {
            await _viewModel.AnswerQuestionCommand.ExecuteAsync(question);
        }
    }

    private async void OnEditAnswerClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is IQuestionDetails question)
        {
            await _viewModel.EditAnswerCommand.ExecuteAsync(question);
        }
    }
}
