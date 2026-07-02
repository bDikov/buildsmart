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
    public List<string> RawOptions { get; set; } = new();

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
        OnPropertyChanged(nameof(AnswerDisplay));
    }

    public bool BoolAnswer
    {
        get => bool.TryParse(Answer, out var result) && result;
        set
        {
            Answer = value.ToString(); 
        }
    }

    public string AnswerDisplay
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Answer)) return string.Empty;
            if (IsChoice)
            {
                var idx = RawOptions.IndexOf(Answer);
                if (idx >= 0 && idx < Options.Count)
                {
                    return Options[idx];
                }
                return Answer;
            }
            if (IsMultiSelect)
            {
                var selectedRaw = Answer.Split(',').Select(a => a.Trim()).ToList();
                var selectedDisplay = new List<string>();
                foreach (var raw in selectedRaw)
                {
                    var idx = RawOptions.IndexOf(raw);
                    if (idx >= 0 && idx < Options.Count)
                    {
                        selectedDisplay.Add(Options[idx]);
                    }
                    else
                    {
                        selectedDisplay.Add(raw);
                    }
                }
                return string.Join(", ", selectedDisplay);
            }
            return Answer;
        }
    }

    public void ToggleMultiSelectOption(int index, bool isSelected)
    {
        if (index < 0 || index >= RawOptions.Count) return;
        var rawOption = RawOptions[index];

        var currentAnswers = string.IsNullOrWhiteSpace(Answer) 
            ? new List<string>() 
            : Answer.Split(',').Select(a => a.Trim()).ToList();
        
        if (isSelected && !currentAnswers.Contains(rawOption))
        {
            currentAnswers.Add(rawOption);
        }
        else if (!isSelected && currentAnswers.Contains(rawOption))
        {
            currentAnswers.Remove(rawOption);
        }
        
        Answer = string.Join(", ", currentAnswers);
    }

    public bool IsOptionSelected(int index)
    {
        if (index < 0 || index >= RawOptions.Count) return false;
        var rawOption = RawOptions[index];
        if (string.IsNullOrWhiteSpace(Answer)) return false;
        var currentAnswers = Answer.Split(',').Select(a => a.Trim()).ToList();
        return currentAnswers.Contains(rawOption);
    }
}


