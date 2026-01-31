using CommunityToolkit.Mvvm.ComponentModel;

namespace BuildSmart.Maui.ViewModels.Admin;

public partial class QuestionViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id;

    [ObservableProperty]
    private string _text;

    [ObservableProperty]
    private string _type; // "text", "number", "boolean"

    [ObservableProperty]
    private bool _isRequired; // Defaults to false

    public QuestionViewModel()
    {
        _id = $"q{Guid.NewGuid().ToString("N").Substring(0, 5)}";
        _text = string.Empty;
        _type = "text";
    }
}
