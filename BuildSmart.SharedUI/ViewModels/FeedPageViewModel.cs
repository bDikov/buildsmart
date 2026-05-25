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
		private ObservableCollection<FeedMediaItem> _feedVideos = new();

		public class FeedMediaItem
		{
			public Guid Id { get; set; } = Guid.NewGuid();
			public string TradesmanId { get; set; } = string.Empty;
			public string VideoUrl { get; set; } = string.Empty;
			public string Name { get; set; } = string.Empty;
			public string Role { get; set; } = string.Empty;
			public string Location { get; set; } = string.Empty;
			public double Rating { get; set; }
			public string ProfilePictureUrl { get; set; } = string.Empty;
		}

		[ObservableProperty]
		private ObservableCollection<IGetAvailableAuctions_AvailableAuctions> _auctions = new();

		[ObservableProperty]
		private bool _isLoading;

		[ObservableProperty]
		private bool _isTradesman;

		[ObservableProperty]
		private bool _isHomeowner = true;

		[ObservableProperty]
		private bool? _hasProjects;

		public class CategoryItem
		{
			public Guid Id { get; set; }
			public string Name { get; set; } = string.Empty;
		}

		[ObservableProperty]
		private ObservableCollection<CategoryItem> _categories = new();

		[ObservableProperty]
		private Guid? _selectedCategoryId;

		[ObservableProperty]
		private bool _isFilterExpanded = false;

		[RelayCommand]
		public void ToggleFilter()
		{
			IsFilterExpanded = !IsFilterExpanded;
		}

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

		private async Task LoadHomeownerProjectsAsync()
		{
			try
			{
				var result = await _apiClient.GetMyProjects.ExecuteAsync();
				if (result.Data?.MyProjects != null && result.Data.MyProjects.Count > 0)
				{
					AppServiceLocator.MainThread.BeginInvokeOnMainThread(() =>
					{
						HasProjects = true;
					});
				}
				else
				{
					AppServiceLocator.MainThread.BeginInvokeOnMainThread(() =>
					{
						HasProjects = false;
					});
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[FeedPageViewModel] LoadHomeownerProjects Error: {ex}");
			}
		}

		[RelayCommand]
		public async Task NavigateToDetailsAsync(object item)
		{
			if (item is IGetTradesmanProfiles_TradesmanProfiles tradesman)
			{
				await AppServiceLocator.Navigation.NavigateToAsync($"/tradesman-profile?TradesmanId={tradesman.Id}");
			}
			else if (item is IGetAvailableAuctions_AvailableAuctions auction)
			{
				await AppServiceLocator.Navigation.NavigateToAsync($"/auction-hub?jobId={auction.Job.Id}");
			}
		}

		[RelayCommand]
		public async Task NavigateToWizardAsync()
		{
			await AppServiceLocator.Navigation.NavigateToAsync("/job-wizard");
		}

		[RelayCommand]
		public async Task NavigateToPassedAuctionsAsync()
		{
			await AppServiceLocator.Navigation.NavigateToAsync("/passed-auctions");
		}

		[RelayCommand]
		public async Task SelectCategoryAsync(Guid? categoryId)
		{
			SelectedCategoryId = categoryId;
			await LoadFeedMediaAsync();
		}

		[RelayCommand]
		public async Task LoadFeedAsync()
		{
			if (IsLoading) return;

			try
			{
				IsLoading = true;

				if (!await EnsureRoleDetectedAsync())
				{
					await AppServiceLocator.MainThread.InvokeOnMainThreadAsync(() =>
					{
						AppServiceLocator.Navigation.NavigateToAsync("//LoginPage");
					});
					return;
				}

				if (IsTradesman)
				{
					await LoadAuctionsAsync();
				}
				else
				{
					await LoadHomeownerProjectsAsync();
					await LoadCategoriesAsync();
					await LoadFeedMediaAsync();
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

		private async Task LoadCategoriesAsync()
		{
			try
			{
				var result = await _apiClient.GetServiceCategories.ExecuteAsync();
				if (result.Data?.ServiceCategories != null)
				{
					AppServiceLocator.MainThread.BeginInvokeOnMainThread(() =>
					{
						Categories.Clear();
						var currentCulture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

						// Filter only Active categories
						foreach (var cat in result.Data.ServiceCategories.Where(c => c.Status == BuildSmart.SharedUI.GraphQL.CategoryStatus.Active))
						{
							// Find translation for current culture, fallback to default English name
							var translation = cat.Translations?.FirstOrDefault(t => string.Equals(t.LanguageCode, currentCulture, StringComparison.OrdinalIgnoreCase));
							var displayName = translation?.Name ?? cat.Name;

							Categories.Add(new CategoryItem 
							{ 
								Id = Guid.Parse(cat.Id.ToString()), 
								Name = displayName 
							});
						}
					});
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[FeedPageViewModel] LoadCategories Error: {ex}");
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

		private async Task LoadFeedMediaAsync()
		{
			try
			{
				// Build filter for HotChocolate
				TradesmanMediaFilterInput? filter = null;
				if (SelectedCategoryId.HasValue)
				{
					filter = new TradesmanMediaFilterInput
					{
						ServiceCategoryId = new UuidOperationFilterInput { Eq = SelectedCategoryId.Value },
						Type = new MediaTypeOperationFilterInput { Eq = MediaType.Video }
					};
				}
				else 
				{
					filter = new TradesmanMediaFilterInput
					{
						Type = new MediaTypeOperationFilterInput { Eq = MediaType.Video }
					};
				}

				var result = await _apiClient.GetFeedMedia.ExecuteAsync(filter);

				if (result.Errors?.Count > 0)
				{
					await AppServiceLocator.Alerts.DisplayAlert("Feed Error", result.Errors[0].Message, "OK");
					return;
				}

				AppServiceLocator.MainThread.BeginInvokeOnMainThread(() =>
				{
					if (result.Data?.FeedMedia is not null)
					{
						FeedVideos.Clear();
						foreach (var media in result.Data.FeedMedia)
						{
							if (media != null)
							{
								FeedVideos.Add(new FeedMediaItem
								{
									Id = Guid.Parse(media.Id.ToString()),
									TradesmanId = media.TradesmanId.ToString(),
									VideoUrl = media.VideoUrl,
									Name = $"{media.TradesmanProfile?.User?.FirstName} {media.TradesmanProfile?.User?.LastName}",
									Role = media.ServiceCategory?.Name ?? media.TradesmanProfile?.Skills?.FirstOrDefault()?.ServiceCategory?.Name ?? "Professional",
									Location = media.TradesmanProfile?.User?.Location ?? "",
									Rating = media.TradesmanProfile?.AverageRating ?? 0,
									ProfilePictureUrl = media.TradesmanProfile?.User?.ProfilePictureUrl ?? ""
								});
							}
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