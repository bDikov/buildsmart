using CommunityToolkit.Mvvm.ComponentModel;

namespace BuildSmart.Maui.ViewModels;

public partial class WizardQuestionViewModel : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = "text"; // text, number, boolean
    public string CategoryName { get; set; } = string.Empty;
    public bool IsRequired { get; set; }

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _answer = string.Empty;
}
