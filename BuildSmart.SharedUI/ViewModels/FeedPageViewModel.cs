using BuildSmart.SharedUI.GraphQL;
using BuildSmart.SharedUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace BuildSmart.SharedUI.ViewModels
{
	public partial class FeedPageViewModel : ObservableObject
	{
		private readonly IBuildSmartApiClient _apiClient;
		private readonly IAuthService _authService;

		[ObservableProperty]
		private ObservableCollection<IGetTradesmanProfiles_TradesmanProfiles> _tradesmen = new();

		[ObservableProperty]
		private ObservableCollection<IGetAvailableAuctions_AvailableAuctions> _auctions = new();

		[ObservableProperty]
		private bool _isLoading;

		[ObservableProperty]
		private bool _isTradesman;

		[ObservableProperty]
		private bool _isHomeowner = true;

		public FeedPageViewModel(IBuildSmartApiClient apiClient, IAuthService authService)
		{
		    _apiClient = apiClient;
		    _authService = authService;
		}

		private async Task<bool> EnsureRoleDetectedAsync()
		{
		    var token = await _authService.GetTokenAsync();
		    if (string.IsNullOrEmpty(token)) return false;

		    var role = _authService.GetUserRoleFromToken(token);

		    // Handle various casing (DB vs JWT vs Enum)
		    IsTradesman = string.Equals(role, "TRADESMAN", StringComparison.OrdinalIgnoreCase) || 
		                  string.Equals(role, "Tradesman", StringComparison.OrdinalIgnoreCase);

		    IsHomeowner = !IsTradesman;
		    return true;
		}

		[RelayCommand]
		public async Task NavigateToDetailsAsync(object item)
		{
		    if (item is IGetTradesmanProfiles_TradesmanProfiles tradesman)
		    {
		        await AppServiceLocator.Navigation.NavigateToAsync($"/{"TradesmanDetailsPage"}?TradesmanId={tradesman.Id}");
		    }
		    else if (item is IGetAvailableAuctions_AvailableAuctions auction)
		    {
		        await AppServiceLocator.Navigation.NavigateToAsync($"/{"AuctionHubPage"}?jobId={auction.Job.Id}");
		    }
		}

		[RelayCommand]
		public async Task NavigateToWizardAsync()
		{
		    await AppServiceLocator.Navigation.NavigateToAsync($"/{"JobWizardPage"}");
		}

		[RelayCommand]
		public async Task NavigateToPassedAuctionsAsync()
		{
			await AppServiceLocator.Navigation.NavigateToAsync("/PassedAuctionsPage");
		}

		[RelayCommand]
		public async Task LoadFeedAsync()
		{
		    if (IsLoading) return;

		    try
		    {
		        IsLoading = true;

		        await EnsureRoleDetectedAsync();

		        if (IsTradesman)
		        {
		            await LoadAuctionsAsync();
		        }
		        else
		        {
		            await LoadTradesmenAsync();
		        }
		    }
		    catch (Exception ex)
		    {
		        await AppServiceLocator.Alerts.DisplayAlert("Initialization Error", ex.Message, "OK");
		    }
		    finally
		    {
		        IsLoading = false;
		    }
		}

		private async Task LoadAuctionsAsync()
		{
			try
			{
				var result = await _apiClient.GetAvailableAuctions.ExecuteAsync();

				if (result.Errors?.Count > 0)
				{
					await AppServiceLocator.Alerts.DisplayAlert("Feed Error", result.Errors[0].Message, "OK");
					return;
				}

                AppServiceLocator.MainThread.BeginInvokeOnMainThread(() =>
                {
				    Auctions.Clear();
				    if (result.Data?.AvailableAuctions is not null)
				    {
					    foreach (var auction in result.Data.AvailableAuctions)
					    {
					        if (auction != null)
						        Auctions.Add(auction);
					    }
				    }
                });
			}
			catch (Exception ex)
			{
				await AppServiceLocator.Alerts.DisplayAlert("Error", ex.Message, "OK");
			}
		}
		
		private async Task LoadTradesmenAsync()
		{
		    try
		    {
			    var result = await _apiClient.GetTradesmanProfiles.ExecuteAsync();
			    
			    if (result.Errors?.Count > 0)
			    {
			        await AppServiceLocator.Alerts.DisplayAlert("Feed Error", result.Errors[0].Message, "OK");
			        return;
			    }
			    
                AppServiceLocator.MainThread.BeginInvokeOnMainThread(() =>
                {
			        if (result.Data?.TradesmanProfiles is not null)
			        {
				        Tradesmen.Clear();
				        foreach (var tradesman in result.Data.TradesmanProfiles)
				        {
				            if (tradesman != null)
					            Tradesmen.Add(tradesman);
				        }
			        }
                });
			}
			catch (Exception ex)
			{
			    await AppServiceLocator.Alerts.DisplayAlert("Error", ex.Message, "OK");
			}
		}

		[RelayCommand]
		private async Task PassAuction(IGetAvailableAuctions_AvailableAuctions auction)
		{
			if (auction == null) return;

			try
			{
				IsLoading = true;

				var userResult = await _apiClient.GetCurrentUser.ExecuteAsync();
				var profileId = userResult.Data?.CurrentUser?.TradesmanProfile?.Id;

				if (profileId == null)
				{
					await AppServiceLocator.Alerts.DisplayAlert("Error", "Could not find tradesman profile.", "OK");
					return;
				}

				var result = await _apiClient.PassAuction.ExecuteAsync(Guid.Parse(profileId), auction.Job.Id);

				if (result.Errors.Count == 0)
				{
				    AppServiceLocator.MainThread.BeginInvokeOnMainThread(() =>
				    {
					    Auctions.Remove(auction);
				    });
				}
			}
			catch (Exception ex)
			{
				await AppServiceLocator.Alerts.DisplayAlert("Error", ex.Message, "OK");
			}
			finally
			{
				IsLoading = false;
			}
		}
	}
}






