using CommunityToolkit.Mvvm.ComponentModel;

namespace BuildSmart.Maui.ViewModels;

public partial class WizardQuestionViewModel : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    
    private string _type = "text";
    public string Type 
    { 
        get => _type; 
        set 
        {
            SetProperty(ref _type, value);
            OnPropertyChanged(nameof(IsText));
            OnPropertyChanged(nameof(IsChoice));
            OnPropertyChanged(nameof(IsBoolean));
        }
    } 

    public string CategoryName { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    
    public List<string> Options { get; set; } = new();

    public bool IsText => Type != "choice" && Type != "boolean";
    public bool IsChoice => Type == "choice";
    public bool IsBoolean => Type == "boolean";

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _answer = string.Empty;

    partial void OnAnswerChanged(string value)
    {
        OnPropertyChanged(nameof(BoolAnswer));
    }

    public bool BoolAnswer
    {
        get => bool.TryParse(Answer, out var result) && result;
        set
        {
            // Set the backing field directly or property? 
            // Setting property triggers OnAnswerChanged -> triggers BoolAnswer change again (loop?)
            // No, because value won't change.
            Answer = value.ToString(); 
        }
    }
}
