using System.Threading.Tasks;

namespace BuildSmart.Maui.Services;

public class AppMediaPicker : BuildSmart.SharedUI.MauiMocks.IMediaPicker
{
    public async Task<BuildSmart.SharedUI.MauiMocks.FileResult?> PickPhotoAsync(BuildSmart.SharedUI.MauiMocks.MediaPickerOptions? options = null)
    {
        var result = await Microsoft.Maui.Media.MediaPicker.Default.PickPhotoAsync();
        if (result == null) return null;

        return new BuildSmart.SharedUI.MauiMocks.FileResult
        {
            FileName = result.FileName,
            FullPath = result.FullPath,
            ContentType = result.ContentType,
            StreamFunc = () => result.OpenReadAsync()
        };
    }

    public async Task<BuildSmart.SharedUI.MauiMocks.FileResult?> PickVideoAsync(BuildSmart.SharedUI.MauiMocks.MediaPickerOptions? options = null)
    {
        var result = await Microsoft.Maui.Media.MediaPicker.Default.PickVideoAsync();
        if (result == null) return null;

        return new BuildSmart.SharedUI.MauiMocks.FileResult
        {
            FileName = result.FileName,
            FullPath = result.FullPath,
            ContentType = result.ContentType,
            StreamFunc = () => result.OpenReadAsync()
        };
    }
}

public class AppFilePicker : BuildSmart.SharedUI.MauiMocks.IFilePicker
{
    public async Task<BuildSmart.SharedUI.MauiMocks.FileResult?> PickAsync(BuildSmart.SharedUI.MauiMocks.PickOptions? options = null)
    {
        var result = await Microsoft.Maui.Storage.FilePicker.Default.PickAsync();
        if (result == null) return null;

        return new BuildSmart.SharedUI.MauiMocks.FileResult
        {
            FileName = result.FileName,
            FullPath = result.FullPath,
            ContentType = result.ContentType,
            StreamFunc = () => result.OpenReadAsync()
        };
    }
}