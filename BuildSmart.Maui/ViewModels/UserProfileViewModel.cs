using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BuildSmart.Maui.ViewModels;

public partial class UserProfileViewModel : ObservableObject
{
    private readonly IBuildSmartApiClient _apiClient;
    private readonly IMediaPicker _mediaPicker;
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

    public UserProfileViewModel(IBuildSmartApiClient apiClient, IMediaPicker mediaPicker)
    {
        _apiClient = apiClient;
        _mediaPicker = mediaPicker;
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
