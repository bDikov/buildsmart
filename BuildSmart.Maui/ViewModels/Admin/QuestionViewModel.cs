using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BuildSmart.Maui.ViewModels.Admin;

public partial class QuestionViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id;

    [ObservableProperty]
    private string _text;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsChoiceType))]
    private string _type; // "text", "number", "boolean", "choice"

    [ObservableProperty]
    private bool _isRequired; // Defaults to false

    [ObservableProperty]
    private ObservableCollection<OptionViewModel> _options = new();

    public bool IsChoiceType => Type?.ToLower() == "choice";

    public List<string> AllQuestionTypes => new() { "text", "number", "boolean", "choice" };

    public QuestionViewModel()
    {
        _id = $"q{Guid.NewGuid().ToString("N").Substring(0, 5)}";
        _text = string.Empty;
        _type = "text";
    }

    [RelayCommand]
    private void AddOption()
    {
        Options.Add(new OptionViewModel($"Option {Options.Count + 1}"));
    }

    [RelayCommand]
    private void RemoveOption(OptionViewModel option)
    {
        if (Options.Contains(option))
        {
            Options.Remove(option);
        }
    }
}
