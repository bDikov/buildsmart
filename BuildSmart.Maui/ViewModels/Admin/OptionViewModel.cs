using CommunityToolkit.Mvvm.ComponentModel;

namespace BuildSmart.Maui.ViewModels.Admin;

public partial class OptionViewModel : ObservableObject
{
    [ObservableProperty]
    private string _value;

    public OptionViewModel(string value)
    {
        _value = value;
    }
}
