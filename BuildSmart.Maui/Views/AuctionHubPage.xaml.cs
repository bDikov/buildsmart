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

    private async void OnEditQuestionClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is IGetAuctionById_AuctionById_Questions question)
        {
            var currentId = _viewModel.CurrentTradesmanProfileId?.ToString();
            var ownerId = question.TradesmanProfileId.ToString();

            // Debug alert to see what's happening
            await Shell.Current.DisplayAlert("Debug ID Check", $"Current: {currentId}\nOwner: {ownerId}", "OK");

            if (string.Equals(currentId, ownerId, StringComparison.OrdinalIgnoreCase))
            {
                await _viewModel.EditQuestionCommand.ExecuteAsync(question);
            }
            else
            {
                await Shell.Current.DisplayAlert("Access Denied", "You can only edit your own questions.", "OK");
            }
        }
    }

    private async void OnReplyQuestionClicked(object sender, TappedEventArgs e)
    {
        try
        {
            var parameter = e.Parameter ?? (sender as BindableObject)?.BindingContext;
            
            System.Diagnostics.Debug.WriteLine($"[DEBUG] OnReplyQuestionClicked fired.");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Sender type: {sender?.GetType().Name ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Parameter type: {parameter?.GetType().Name ?? "null"}");

            if (parameter is IGetAuctionById_AuctionById_Questions question)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Routing to Top-Level Question Reply. QuestionId: {question.Id}");
                await _viewModel.ReplyToQuestionCommand.ExecuteAsync(question);
            }
            else if (parameter is IGetAuctionById_AuctionById_Questions_Replies reply)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Routing to Nested Reply. ReplyId: {reply.Id}");
                await _viewModel.ReplyToNestedQuestionCommand.ExecuteAsync(reply);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Unrecognized parameter type. Cannot execute command.");
                await Shell.Current.DisplayAlert("Debug", $"Unrecognized parameter type: {parameter?.GetType().Name}", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Error in OnReplyQuestionClicked: {ex}");
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}
