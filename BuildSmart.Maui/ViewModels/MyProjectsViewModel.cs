using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BuildSmart.Maui.ViewModels;

public partial class MyProjectsViewModel : ObservableObject
{
    private readonly IBuildSmartApiClient _apiClient;

    [ObservableProperty]
    private ObservableCollection<IGetMyProjects_MyProjects> _projects = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isEmpty;

    public MyProjectsViewModel(IBuildSmartApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [RelayCommand]
    private async Task CreateProjectAsync()
    {
        await Shell.Current.GoToAsync(nameof(Views.JobWizardPage));
    }

    [RelayCommand]
    public async Task LoadProjectsAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            var result = await _apiClient.GetMyProjects.ExecuteAsync();

            if (result.Errors.Count > 0)
            {
                var error = result.Errors.First();
                await Shell.Current.DisplayAlert("GraphQL Error", $"{error.Message}\nCode: {error.Code}", "OK");
                return;
            }

            Projects.Clear();
            if (result.Data?.MyProjects != null)
            {
                foreach (var project in result.Data.MyProjects)
                {
                    Projects.Add(project);
                }
            }

            IsEmpty = Projects.Count == 0;
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
