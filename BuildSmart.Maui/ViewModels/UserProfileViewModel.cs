using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using BuildSmart.Maui.Services;

namespace BuildSmart.Maui.ViewModels;

public partial class UserProfileViewModel : ObservableObject
{
    private readonly IBuildSmartApiClient _apiClient;
    private readonly IMediaPicker _mediaPicker;
    private readonly IFilePicker _filePicker;
    private readonly IFileService _fileService;
    private Guid _userId;

    [ObservableProperty]
    private string _firstName = string.Empty;

    [ObservableProperty]
    private string _lastName = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty; // Read-only usually

    [ObservableProperty]
    private string _bio = string.Empty;

    [ObservableProperty]
    private string _location = string.Empty;

    [ObservableProperty]
    private string _profilePictureUrl = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isTradesman;

    [ObservableProperty]
    private string? _videoIntroductionUrl;

    public ObservableCollection<IGetCurrentUser_CurrentUser_TradesmanProfile_PortfolioEntries> PortfolioEntries { get; } = new();
    public ObservableCollection<IGetCurrentUser_CurrentUser_TradesmanProfile_Certifications> Certifications { get; } = new();

    public UserProfileViewModel(IBuildSmartApiClient apiClient, IMediaPicker mediaPicker, IFilePicker filePicker, IFileService fileService)
    {
        _apiClient = apiClient;
        _mediaPicker = mediaPicker;
        _filePicker = filePicker;
        _fileService = fileService;
        LoadProfileAsync();
    }

    [RelayCommand]
    private async Task ChangePhotoAsync()
    {
        try
        {
            var photo = await _mediaPicker.PickPhotoAsync();
            if (photo == null) return;

            using var stream = await photo.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            
            var base64 = Convert.ToBase64String(memoryStream.ToArray());
            var mimeType = photo.ContentType ?? "image/jpeg"; // Default fallback
            
            // Format: data:image/png;base64,.....
            ProfilePictureUrl = $"data:{mimeType};base64,{base64}";
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to pick photo: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task AddPortfolioItemAsync()
    {
        try
        {
            var photo = await _mediaPicker.PickPhotoAsync(new MediaPickerOptions { Title = "Select Portfolio Image" });
            if (photo == null) return;

            string title = await Shell.Current.DisplayPromptAsync("Portfolio", "Enter a title for this work:", "Upload", "Cancel", "Kitchen Renovation...");
            if (string.IsNullOrWhiteSpace(title)) return;

            IsBusy = true;
            using var stream = await photo.OpenReadAsync();
            var result = await _fileService.UploadPortfolioEntryAsync(title, null, stream, photo.FileName);

            if (result != null)
            {
                await Shell.Current.DisplayAlert("Success", "Portfolio item added.", "OK");
                await LoadProfileAsync();
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Failed to upload portfolio item.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddCertificationAsync()
    {
        try
        {
            var file = await _filePicker.PickAsync(new PickOptions { PickerTitle = "Select Certificate (PDF or Image)" });
            if (file == null) return;

            string title = await Shell.Current.DisplayPromptAsync("Certification", "Enter certificate name:", "Next", "Cancel", "Master Plumber License...");
            if (string.IsNullOrWhiteSpace(title)) return;

            IsBusy = true;
            using var stream = await file.OpenReadAsync();
            // Defaulting issued date to today for simplicity in this MVP prompt flow
            var result = await _fileService.UploadCertificationAsync(title, null, DateTime.UtcNow, null, stream, file.FileName);

            if (result != null)
            {
                await Shell.Current.DisplayAlert("Success", "Certification added.", "OK");
                await LoadProfileAsync();
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Failed to upload certification.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ChangeVideoIntroAsync()
    {
        try
        {
            var video = await _mediaPicker.PickVideoAsync();
            if (video == null) return;

            IsBusy = true;
            using var stream = await video.OpenReadAsync();
            var result = await _fileService.UpdateVideoIntroductionAsync(stream, video.FileName);

            if (result != null)
            {
                await Shell.Current.DisplayAlert("Success", "Video introduction updated.", "OK");
                await LoadProfileAsync();
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Failed to upload video.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LoadProfileAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _apiClient.GetCurrentUser.ExecuteAsync();

            if (result.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", result.Errors.First().Message, "OK");
                return;
            }

            if (result.Data?.CurrentUser is not null)
            {
                var user = result.Data.CurrentUser;
                _userId = user.Id;
                FirstName = user.FirstName;
                LastName = user.LastName;
                Email = user.Email;
                Bio = user.Bio ?? string.Empty;
                Location = user.Location ?? string.Empty;
                ProfilePictureUrl = user.ProfilePictureUrl ?? string.Empty;

                IsTradesman = user.TradesmanProfile != null;
                if (user.TradesmanProfile != null)
                {
                    VideoIntroductionUrl = user.TradesmanProfile.VideoIntroductionUrl;
                    
                    PortfolioEntries.Clear();
                    foreach (var entry in user.TradesmanProfile.PortfolioEntries)
                        PortfolioEntries.Add(entry);

                    Certifications.Clear();
                    foreach (var cert in user.TradesmanProfile.Certifications)
                        Certifications.Add(cert);
                }
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            var result = await _apiClient.UpdateUserProfile.ExecuteAsync(
                _userId,
                FirstName,
                LastName,
                Bio,
                Location,
                ProfilePictureUrl
            );

            if (result.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", result.Errors.First().Message, "OK");
                return;
            }

            await Shell.Current.DisplayAlert("Success", "Profile updated successfully.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
