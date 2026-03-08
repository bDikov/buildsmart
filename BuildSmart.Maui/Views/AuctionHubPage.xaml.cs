using BuildSmart.Maui.ViewModels;
using BuildSmart.Maui.GraphQL;

namespace BuildSmart.Maui.Views;

public partial class AuctionHubPage : ContentPage
{
    private readonly AuctionHubViewModel _viewModel;

	public AuctionHubPage(AuctionHubViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await _viewModel.CleanupAsync();
    }

    private async void OnEditQuestionClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is IQuestionDetails question)
        {
            await _viewModel.EditQuestionCommand.ExecuteAsync(question);
        }
    }

    private async void OnEditNestedQuestionClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is IQuestionReplyDetails reply)
        {
            await _viewModel.EditNestedQuestionCommand.ExecuteAsync(reply);
        }
    }

    private async void OnEditAnswerClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is IQuestionDetails question)
        {
            await _viewModel.EditAnswerCommand.ExecuteAsync(question);
        }
    }

    private async void OnReplyQuestionClicked(object sender, TappedEventArgs e)
    {
        try
        {
            var parameter = e.Parameter ?? (sender as BindableObject)?.BindingContext;
            
            if (parameter is IQuestionDetails question)
            {
                await _viewModel.ReplyToQuestionCommand.ExecuteAsync(question);
            }
            else if (parameter is IQuestionReplyDetails reply)
            {
                await _viewModel.ReplyToNestedQuestionCommand.ExecuteAsync(reply);
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}
