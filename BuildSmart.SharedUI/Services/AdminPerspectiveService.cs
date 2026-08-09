using System;

namespace BuildSmart.SharedUI.Services;

public enum AdminPerspectiveMode
{
    Admin,
    Homeowner,
    Tradesman
}

public interface IAdminPerspectiveService
{
    AdminPerspectiveMode CurrentPerspective { get; set; }
    event Action? PerspectiveChanged;
    void SetPerspective(AdminPerspectiveMode mode);
    bool IsAdminPerspective { get; }
    bool IsHomeownerPerspective { get; }
    bool IsTradesmanPerspective { get; }
}

public class AdminPerspectiveService : IAdminPerspectiveService
{
    private AdminPerspectiveMode _currentPerspective = AdminPerspectiveMode.Admin;

    public AdminPerspectiveMode CurrentPerspective
    {
        get => _currentPerspective;
        set
        {
            if (_currentPerspective != value)
            {
                _currentPerspective = value;
                PerspectiveChanged?.Invoke();
            }
        }
    }

    public event Action? PerspectiveChanged;

    public void SetPerspective(AdminPerspectiveMode mode)
    {
        CurrentPerspective = mode;
    }

    public bool IsAdminPerspective => CurrentPerspective == AdminPerspectiveMode.Admin;
    public bool IsHomeownerPerspective => CurrentPerspective == AdminPerspectiveMode.Homeowner;
    public bool IsTradesmanPerspective => CurrentPerspective == AdminPerspectiveMode.Tradesman;
}
