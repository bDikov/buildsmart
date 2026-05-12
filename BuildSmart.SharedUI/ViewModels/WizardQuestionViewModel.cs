using CommunityToolkit.Mvvm.ComponentModel;

namespace BuildSmart.SharedUI.ViewModels;

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
            OnPropertyChanged(nameof(IsNumber));
            OnPropertyChanged(nameof(IsMultiSelect));
        }
    } 

    public string CategoryName { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    
    public List<string> Options { get; set; } = new();

    public bool IsText => Type != "choice" && Type != "boolean" && Type != "number" && Type != "multiselect";
    public bool IsChoice => Type == "choice";
    public bool IsBoolean => Type == "boolean";
    public bool IsNumber => Type == "number";
    public bool IsMultiSelect => Type == "multiselect";

    [ObservableProperty]
    private string _dependsOn = string.Empty;

    [ObservableProperty]
    private string _dependsOnValue = string.Empty;

    [ObservableProperty]
    private string _hintText = string.Empty;

    [ObservableProperty]
    private bool _isVisible = true;

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
            Answer = value.ToString(); 
        }
    }

    public void ToggleMultiSelectOption(string option, bool isSelected)
    {
        var currentAnswers = string.IsNullOrWhiteSpace(Answer) ? new List<string>() : Answer.Split(',').Select(a => a.Trim()).ToList();
        
        if (isSelected && !currentAnswers.Contains(option))
        {
            currentAnswers.Add(option);
        }
        else if (!isSelected && currentAnswers.Contains(option))
        {
            currentAnswers.Remove(option);
        }
        
        Answer = string.Join(", ", currentAnswers);
    }
}


