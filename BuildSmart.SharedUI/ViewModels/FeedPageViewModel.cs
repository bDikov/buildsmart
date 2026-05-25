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

		// Cache to hold all loaded videos for infinite looping
		private List<FeedMediaItem> _cachedVideos = new();

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

						// Filter only Active categories and exclude Global category
						foreach (var cat in result.Data.ServiceCategories.Where(c => c.Status == BuildSmart.SharedUI.GraphQL.CategoryStatus.Active && c.IsGlobal != true))
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

		private int _currentSkip = 0;
		private const int PageSize = 3;
		public bool HasNextPage { get; private set; } = true;
		private bool _isLoadingMore = false;

		private async Task LoadFeedMediaAsync()
		{
			try
			{
				_currentSkip = 0;
				HasNextPage = true;
				_cachedVideos.Clear();
				
				var result = await FetchFeedMediaBatchAsync(_currentSkip, PageSize);
				if (result?.Items != null)
				{
					AppServiceLocator.MainThread.BeginInvokeOnMainThread(() =>
					{
						FeedVideos.Clear();
						// Reverse the list when adding so the first item from the DB is at the end of the collection (Top of the Stack in UI)
						foreach (var media in result.Items.Reverse())
						{
							if (media != null)
							{
								var item = CreateFeedMediaItem(media);
								_cachedVideos.Insert(0, item); // Keep original order in cache
								FeedVideos.Add(item);
							}
						}
					});
					
					_currentSkip += PageSize;
					HasNextPage = result.PageInfo.HasNextPage;
				}
			}
			catch (Exception ex)
			{
				await AppServiceLocator.Alerts.DisplayAlert("Error", ex.Message, "OK");
			}
		}

		public async Task LoadMoreFeedMediaAsync()
		{
			if (_isLoadingMore) return;

			try
			{
				_isLoadingMore = true;
				
				if (HasNextPage)
				{
					var result = await FetchFeedMediaBatchAsync(_currentSkip, PageSize);
					if (result?.Items != null)
					{
						AppServiceLocator.MainThread.BeginInvokeOnMainThread(() =>
						{
							// Insert at 0 so they appear at the bottom of the Tinder stack
							foreach (var media in result.Items)
							{
								if (media != null)
								{
									var videoItem = CreateFeedMediaItem(media);
									_cachedVideos.Add(videoItem);

									// Strict deduplication check to prevent Blazor @key crashes
									if (!FeedVideos.Any(v => v.Id == videoItem.Id))
									{
										FeedVideos.Insert(0, videoItem);
									}
								}
							}

							TrimFeedStack();
						});

						_currentSkip += PageSize;
						HasNextPage = result.PageInfo.HasNextPage;
					}
				}
				else if (_cachedVideos.Count > 0)
				{
					// We've exhausted the API, start looping from the cache
					AppServiceLocator.MainThread.BeginInvokeOnMainThread(() =>
					{
						// Calculate our virtual skip based on modulo
						int virtualSkip = _currentSkip % _cachedVideos.Count;
						var cachedBatch = _cachedVideos.Skip(virtualSkip).Take(PageSize).ToList();
						
						// If we reached the end of the cache mid-batch, wrap around to the start
						if (cachedBatch.Count < PageSize)
						{
							cachedBatch.AddRange(_cachedVideos.Take(PageSize - cachedBatch.Count));
						}

						foreach (var videoItem in cachedBatch)
						{
							// Create a fresh clone so Blazor @key and JS observer see it as a "new" DOM element in the stack
							var clonedItem = new FeedMediaItem
							{
								Id = Guid.NewGuid(), // NEW ID for the loop
								TradesmanId = videoItem.TradesmanId,
								VideoUrl = videoItem.VideoUrl,
								Name = videoItem.Name,
								Role = videoItem.Role,
								Location = videoItem.Location,
								Rating = videoItem.Rating,
								ProfilePictureUrl = videoItem.ProfilePictureUrl
							};
							
							FeedVideos.Insert(0, clonedItem);
						}

						TrimFeedStack();
						_currentSkip += PageSize;
					});
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error loading more videos: {ex.Message}");
			}
			finally 
			{
				_isLoadingMore = false;
			}
		}

		private void TrimFeedStack()
		{
			while (FeedVideos.Count > 10)
			{
				// Remove the absolute bottom-most (furthest away) video if we somehow bloated
				FeedVideos.RemoveAt(0);
			}
		}

		[RelayCommand]
		public void RestartFeedFromCache()
		{
			AppServiceLocator.MainThread.BeginInvokeOnMainThread(() =>
			{
				FeedVideos.Clear();
				// Restore the initial set of videos from the cache, maintaining the reverse order logic for the stack
				var initialBatch = _cachedVideos.Take(PageSize).Reverse().ToList();
				foreach (var video in initialBatch)
				{
					FeedVideos.Add(video);
				}
				
				// Reset pagination state so we can simulate fetching more from cache
				_currentSkip = initialBatch.Count;
				HasNextPage = _currentSkip < _cachedVideos.Count;
			});
		}

		private async Task<IGetFeedMedia_FeedMedia?> FetchFeedMediaBatchAsync(int skip, int take)
		{
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

			var result = await _apiClient.GetFeedMedia.ExecuteAsync(filter, skip, take);

			if (result.Errors?.Count > 0)
			{
				await AppServiceLocator.Alerts.DisplayAlert("Feed Error", result.Errors[0].Message, "OK");
				return null;
			}

			return result.Data?.FeedMedia;
		}

		private void AddVideoToFeed(IGetFeedMedia_FeedMedia_Items media)
		{
			FeedVideos.Add(CreateFeedMediaItem(media));
		}

		private FeedMediaItem CreateFeedMediaItem(IGetFeedMedia_FeedMedia_Items media)
		{
			return new FeedMediaItem
			{
				Id = Guid.Parse(media.Id.ToString()),
				TradesmanId = media.TradesmanId.ToString(),
				VideoUrl = media.VideoUrl,
				Name = $"{media.TradesmanProfile?.User?.FirstName} {media.TradesmanProfile?.User?.LastName}",
				Role = media.ServiceCategory?.Name ?? media.TradesmanProfile?.Skills?.FirstOrDefault()?.ServiceCategory?.Name ?? "Professional",
				Location = media.TradesmanProfile?.User?.Location ?? "",
				Rating = media.TradesmanProfile?.AverageRating ?? 0,
				ProfilePictureUrl = media.TradesmanProfile?.User?.ProfilePictureUrl ?? ""
			};
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