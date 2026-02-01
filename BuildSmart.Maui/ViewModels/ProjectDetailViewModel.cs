using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BuildSmart.Maui.ViewModels;

public partial class ProjectDetailViewModel : ObservableObject, IQueryAttributable
{
    [ObservableProperty]
    private IGetMyProjects_MyProjects? _project;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Project", out var projectObj) && projectObj is IGetMyProjects_MyProjects project)
        {
            Project = project;
        }
    }
}
