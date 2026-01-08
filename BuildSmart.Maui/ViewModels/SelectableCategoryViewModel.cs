using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BuildSmart.Maui.ViewModels;

public partial class SelectableCategoryViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    public IGetServiceCategories_ServiceCategories Category { get; }

    public SelectableCategoryViewModel(IGetServiceCategories_ServiceCategories category)
    {
        Category = category;
    }
}
