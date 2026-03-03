using BuildSmart.Maui.ViewModels;
using BuildSmart.Maui.GraphQL;

namespace BuildSmart.Maui.Views;

public partial class ProjectDetailPage : ContentPage
{
    private readonly ProjectDetailViewModel _viewModel;

    public ProjectDetailPage(ProjectDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    private async void OnReplyToAdminClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is IGetMyProjects_MyProjects_JobPosts job)
        {
            await _viewModel.RespondToAdminCommand.ExecuteAsync(job);
        }
    }

    private async void OnAnswerQuestionClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is IGetMyProjects_MyProjects_JobPosts_Questions question)
        {
            await _viewModel.AnswerQuestionCommand.ExecuteAsync(question);
        }
    }

    private async void OnEditAnswerClicked(object sender, EventArgs e)
    {
        try
        {
            if (sender is Button button && button.CommandParameter != null)
            {
                var param = button.CommandParameter;
                if (param is IGetMyProjects_MyProjects_JobPosts_Questions question)
                {
                    await _viewModel.EditAnswerCommand.ExecuteAsync(question);
                }
                else
                {
                    await Shell.Current.DisplayAlert("Debug", $"Type mismatch: {param.GetType().Name}. Expected IGetMyProjects_MyProjects_JobPosts_Questions", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error in Handler", ex.Message, "OK");
        }
    }

    private async void OnEditAnswersClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is IGetMyProjects_MyProjects_JobPosts job)
        {
            await _viewModel.EditAnswersCommand.ExecuteAsync(job);
        }
    }

    private async void OnReviewScopeClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is IGetMyProjects_MyProjects_JobPosts job)
        {
            await _viewModel.ReviewScopeCommand.ExecuteAsync(job);
        }
    }

    private async void OnReplyQuestionClicked(object sender, TappedEventArgs e)
    {
        try
        {
            var parameter = e.Parameter ?? (sender as BindableObject)?.BindingContext;
            
            if (parameter is IGetMyProjects_MyProjects_JobPosts_Questions question)
            {
                await _viewModel.ReplyToQuestionCommand.ExecuteAsync(question);
            }
            else if (parameter is IGetMyProjects_MyProjects_JobPosts_Questions_Replies reply)
            {
                await _viewModel.ReplyToNestedQuestionCommand.ExecuteAsync(reply);
            }
            else
            {
                await Shell.Current.DisplayAlert("Debug", $"Unrecognized parameter type: {parameter?.GetType().Name}", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnReplyFeedbackClicked(object sender, TappedEventArgs e)
    {
        try
        {
            var parameter = e.Parameter ?? (sender as BindableObject)?.BindingContext;

            if (parameter is IGetMyProjects_MyProjects_JobPosts_Feedbacks feedback)
            {
                await _viewModel.ReplyToFeedbackCommand.ExecuteAsync(feedback);
            }
            else if (parameter is IGetMyProjects_MyProjects_JobPosts_Feedbacks_Replies reply)
            {
                await _viewModel.ReplyToNestedFeedbackCommand.ExecuteAsync(reply);
            }
            else
            {
                await Shell.Current.DisplayAlert("Debug", $"Unrecognized parameter type: {parameter?.GetType().Name}", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}
