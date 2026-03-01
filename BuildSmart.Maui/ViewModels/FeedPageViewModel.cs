using BuildSmart.Maui.GraphQL;
using BuildSmart.Maui.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace BuildSmart.Maui.ViewModels
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
		        await Shell.Current.GoToAsync($"{nameof(Views.TradesmanDetailsPage)}?TradesmanId={tradesman.Id}");
		    }
		    else if (item is IGetAvailableAuctions_AvailableAuctions auction)
		    {
		        await Shell.Current.GoToAsync($"{nameof(Views.AuctionHubPage)}?jobId={auction.Job.Id}");
		    }
		}

		[RelayCommand]
		public async Task NavigateToWizardAsync()
		{
		    await Shell.Current.GoToAsync($"/{nameof(Views.JobWizardPage)}");
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

				if (result.Errors.Count > 0)
				{
					await Shell.Current.DisplayAlert("Feed Error", result.Errors[0].Message, "OK");
					return;
				}

				Auctions.Clear();
				if (result.Data?.AvailableAuctions is not null)
				{
					foreach (var auction in result.Data.AvailableAuctions)
					{
						Auctions.Add(auction);
					}
				}
			}
			catch (Exception ex)
			{
				await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
			}
		}
		private async Task LoadTradesmenAsync()
		{
			var result = await _apiClient.GetTradesmanProfiles.ExecuteAsync();
			if (result.Errors.Count == 0 && result.Data?.TradesmanProfiles is not null)
			{
				Tradesmen.Clear();
				foreach (var tradesman in result.Data.TradesmanProfiles)
				{
					Tradesmen.Add(tradesman);
				}
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
					await Shell.Current.DisplayAlert("Error", "Could not find tradesman profile.", "OK");
					return;
				}

				var result = await _apiClient.PassAuction.ExecuteAsync(profileId.Value, auction.Job.Id);

				if (result.Errors.Count == 0)
				{
					Auctions.Remove(auction);
				}
			}
			catch (Exception ex)
			{
				await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
			}
			finally
			{
				IsLoading = false;
			}
		}
	}
}