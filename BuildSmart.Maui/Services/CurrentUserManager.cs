using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BuildSmart.Maui.Services;

public class CurrentUserManager : INotifyPropertyChanged
{
    private static readonly CurrentUserManager _instance = new();
    public static CurrentUserManager Instance => _instance;

    private Guid? _currentUserId;
    public Guid? CurrentUserId
    {
        get => _currentUserId;
        set
        {
            if (_currentUserId != value)
            {
                _currentUserId = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
