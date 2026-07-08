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

		// --- CATEGORY LRU CACHE SYSTEM ---
		public class CategoryCacheState
		{
			public Guid CategoryId { get; set; }
			public List<FeedMediaItem> CachedVideos { get; set; } = new();
			public int CurrentSkip { get; set; } = 0;
			public bool HasNextPage { get; set; } = true;
			public DateTime LastAccessed { get; set; } = DateTime.UtcNow;
		}

		private const int MaxCachedCategories = 5;
		// Use Guid.Empty for the "All" category (when SelectedCategoryId is null)
		private Dictionary<Guid, CategoryCacheState> _categoryCaches = new();

		private CategoryCacheState GetActiveCacheState()
		{
			Guid key = SelectedCategoryId ?? Guid.Empty;
			
			if (!_categoryCaches.ContainsKey(key))
			{
				// Memory Management: Enforce LRU Limit before adding a new one
				if (_categoryCaches.Count >= MaxCachedCategories)
				{
					var oldestKey = _categoryCaches.OrderBy(c => c.Value.LastAccessed).First().Key;
					_categoryCaches.Remove(oldestKey);
				}

				_categoryCaches[key] = new CategoryCacheState { CategoryId = key };
			}

			// Update access time for LRU priority
			var state = _categoryCaches[key];
			state.LastAccessed = DateTime.UtcNow;
			return state;
		}

		public bool HasNextPage => GetActiveCacheState().HasNextPage;
		// -----------------------------------

		public class FeedMediaItem
		{
			public Guid Id { get; set; } = Guid.NewGuid();
			public string TradesmanId { get; set; } = string.Empty;
			public string VideoUrl { get; set; } = string.Empty;
			public string? MobileVideoUrl { get; set; }
			public string? ImageUrl { get; set; }
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

		[ObservableProperty]
		private Guid? _activeVideoId;

		public FeedPageViewModel(IBuildSmartApiClient apiClient, IAuthService authService)
		{
			_apiClient = apiClient;
			_authService = authService;
		}

		private async Task<bool> EnsureRoleDetectedAsync()
		{
			var token = await _authService.GetTokenAsync();
			if (string.IsNullOrEmpty(token))
			{
				IsTradesman = false;
				IsHomeowner = true;
				return true;
			}

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
				if (result.Data?.MyProjects != null && result.Data.MyProjects.Any(p => p.Title != "Support Chat" && !p.Title.StartsWith("Support - ")))
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
			
			// Visually wipe the UI stack so we don't accidentally display the old category's videos
			// while the new category cache is being resolved
			AppServiceLocator.MainThread.BeginInvokeOnMainThread(() =>
			{
				FeedVideos.Clear();
			});
			
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
					var token = await _authService.GetTokenAsync();
					if (!string.IsNullOrEmpty(token))
					{
						await LoadHomeownerProjectsAsync();
					}
					else
					{
						HasProjects = false;
					}
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

		private static bool IsProjectDetailsCategory(string? templateStructure)
		{
			if (string.IsNullOrWhiteSpace(templateStructure)) return false;
			try
			{
				var node = System.Text.Json.Nodes.JsonNode.Parse(templateStructure);
				return node?["isProjectDetails"]?.GetValue<bool>() ?? false;
			}
			catch
			{
				return false;
			}
		}

		private async Task LoadCategoriesAsync()
		{
			try
			{
				var result = await _apiClient.GetServiceCategories.ExecuteAsync();
				if (result.Data?.ServiceCategories != null)
				{
					// Fetch active feed media to check which categories actually have videos
					var mediaResult = await _apiClient.GetFeedMedia.ExecuteAsync(null, 0, 1000);
					var activeCategoryIds = new HashSet<Guid>();
					if (mediaResult.Data?.FeedMedia?.Items != null)
					{
						foreach (var item in mediaResult.Data.FeedMedia.Items)
						{
							if (item?.ServiceCategoryId.HasValue == true)
							{
								activeCategoryIds.Add(item.ServiceCategoryId.Value);
							}
						}
					}

					AppServiceLocator.MainThread.BeginInvokeOnMainThread(() =>
					{
						Categories.Clear();
						var currentCulture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

						// Filter only Active categories of CategorySpecific type and has active videos
						foreach (var cat in result.Data.ServiceCategories.Where(c => c.Status == BuildSmart.SharedUI.GraphQL.CategoryStatus.Active && c.Type == CategoryType.CategorySpecific))
						{
							var catGuid = Guid.Parse(cat.Id.ToString());
							if (!activeCategoryIds.Contains(catGuid))
							{
								continue; // Skip categories that have no active videos!
							}

							// Find translation for current culture, fallback to default Bulgarian name
							var displayName = currentCulture.StartsWith("en", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(cat.EnglishName) ? cat.EnglishName : cat.Name;

							Categories.Add(new CategoryItem 
							{ 
								Id = catGuid, 
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

		private const int PageSize = 3;
		private bool _isLoadingMore = false;

		private async Task LoadFeedMediaAsync()
		{
			var state = GetActiveCacheState();

			if (state.CachedVideos.Count > 0 && FeedVideos.Count > 0)
			{
				// Cache already exists (user navigated back to the page), do not re-fetch.
				return;
			}

			try
			{
				state.CurrentSkip = 0;
				state.HasNextPage = true;
				state.CachedVideos.Clear();
				
				var result = await FetchFeedMediaBatchAsync(state.CurrentSkip, PageSize);
				if (result?.Items != null)
				{
					AppServiceLocator.MainThread.BeginInvokeOnMainThread(() =>
					{
						FeedVideos.Clear();
						foreach (var media in result.Items)
						{
							if (media != null)
							{
								var item = CreateFeedMediaItem(media);
								state.CachedVideos.Add(item);
								FeedVideos.Add(item);
							}
						}
					});
					
					state.CurrentSkip += result.Items.Count;
					state.HasNextPage = result.PageInfo.HasNextPage;
				}
			}
			catch (Exception ex)
			{
				await AppServiceLocator.Alerts.DisplayAlert("Error", ex.Message, "OK");
			}
		}

		public bool ShouldLoadMore()
		{
			var state = GetActiveCacheState();
			if (!state.HasNextPage || _isLoadingMore) return false;

			var activeId = ActiveVideoId ?? FeedVideos.FirstOrDefault()?.Id;
			if (activeId == null) return true;

			var activeVideo = FeedVideos.FirstOrDefault(v => v.Id == activeId);
			if (activeVideo == null) return true;

			int index = state.CachedVideos.IndexOf(activeVideo);
			if (index == -1) return true;

			int remaining = state.CachedVideos.Count - 1 - index;
			return remaining < 3;
		}

		public async Task LoadMoreFeedMediaAsync()
		{
			var state = GetActiveCacheState();
			if (_isLoadingMore || !state.HasNextPage) return;

			try
			{
				_isLoadingMore = true;
				
				var result = await FetchFeedMediaBatchAsync(state.CurrentSkip, PageSize);
				if (result?.Items != null)
				{
					AppServiceLocator.MainThread.BeginInvokeOnMainThread(() =>
					{
						foreach (var media in result.Items)
						{
							if (media != null)
							{
								var videoItem = CreateFeedMediaItem(media);
								state.CachedVideos.Add(videoItem);

								// Strict deduplication check to prevent Blazor @key crashes
								if (!FeedVideos.Any(v => v.Id == videoItem.Id))
								{
									FeedVideos.Add(videoItem);
								}
							}
						}
					});

					state.CurrentSkip += result.Items.Count;
					state.HasNextPage = result.PageInfo.HasNextPage;
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

		public void LoopNextVideoFromCache()
		{
			var state = GetActiveCacheState();
			if (state.CachedVideos.Count == 0) return;

			AppServiceLocator.MainThread.BeginInvokeOnMainThread(() =>
			{
				int virtualIndex = state.CurrentSkip % state.CachedVideos.Count;
				var nextVideoToLoop = state.CachedVideos[virtualIndex];

				// Reuse the EXACT same object. By doing this in the same render cycle as the remove,
				// Blazor moves the existing DOM node instead of destroying it. This prevents the video 
				// from buffering/re-downloading and perfectly preserves the Plyr instance!
				if (!FeedVideos.Contains(nextVideoToLoop))
				{
					FeedVideos.Insert(0, nextVideoToLoop);
				}
				
				state.CurrentSkip++;
				TrimFeedStack();
			});
		}

		private void TrimFeedStack()
		{
			while (FeedVideos.Count > 10)
			{
				// Remove the absolute bottom-most (furthest away) video if we somehow bloated
				FeedVideos.RemoveAt(0);
			}
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

		private string NormalizeLocalUrl(string? url)
		{
			if (string.IsNullOrEmpty(url)) return string.Empty;

			// If it's a relative URL, prepend the API BaseUrl
			if (url.StartsWith("/"))
			{
				var config = BuildSmart.SharedUI.Services.AppServiceLocator.ServiceResolver?.Invoke(typeof(Microsoft.Extensions.Configuration.IConfiguration)) as Microsoft.Extensions.Configuration.IConfiguration;
				var baseUrl = config?["ApiConfig:BaseUrl"] ?? "https://localhost:7212";
				return $"{baseUrl.TrimEnd('/')}{url}";
			}

			// If it contains http://localhost:5086 (API HTTP port) or http://localhost:5486 (IIS Express HTTP port),
			// map it to the corresponding HTTPS endpoint to prevent Schemeful Same-Site issues
			if (url.StartsWith("http://localhost:5086", StringComparison.OrdinalIgnoreCase))
			{
				return url.Replace("http://localhost:5086", "https://localhost:7212");
			}
			if (url.StartsWith("http://localhost:5486", StringComparison.OrdinalIgnoreCase))
			{
				return url.Replace("http://localhost:5486", "https://localhost:44378");
			}

			return url;
		}

		private FeedMediaItem CreateFeedMediaItem(IGetFeedMedia_FeedMedia_Items media)
		{
			return new FeedMediaItem
			{
				Id = Guid.Parse(media.Id.ToString()),
				TradesmanId = media.TradesmanId.ToString(),
				VideoUrl = NormalizeLocalUrl(media.VideoUrl),
				MobileVideoUrl = NormalizeLocalUrl(media.MobileVideoUrl),
				ImageUrl = NormalizeLocalUrl(media.ImageUrl),
				Name = $"{media.TradesmanProfile?.User?.FirstName} {media.TradesmanProfile?.User?.LastName}",
				Role = media.ServiceCategory?.Name ?? media.TradesmanProfile?.Skills?.FirstOrDefault()?.ServiceCategory?.Name ?? "Professional",
				Location = media.TradesmanProfile?.User?.Location ?? "",
				Rating = media.TradesmanProfile?.AverageRating ?? 0,
				ProfilePictureUrl = NormalizeLocalUrl(media.TradesmanProfile?.User?.ProfilePictureUrl ?? "")
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