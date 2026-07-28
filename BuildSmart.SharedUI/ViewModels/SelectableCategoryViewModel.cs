using BuildSmart.SharedUI.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BuildSmart.SharedUI.ViewModels;

public partial class SelectableCategoryViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    public IGetServiceCategories_ServiceCategories Category { get; }

    public string Icon
    {
        get
        {
            if (Category == null) return "";
            if (!string.IsNullOrEmpty(Category.TemplateStructure))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(Category.TemplateStructure);
                    if (doc.RootElement.TryGetProperty("icon", out var iconProp))
                    {
                        return iconProp.GetString() ?? "";
                    }
                }
                catch {}
            }
            return "";
        }
    }

    public SelectableCategoryViewModel(IGetServiceCategories_ServiceCategories category)
    {
        Category = category;
    }
}
