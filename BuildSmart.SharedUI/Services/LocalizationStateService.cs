using System;

namespace BuildSmart.SharedUI.Services;

public class LocalizationStateService : ILocalizationStateService
{
    public event Action? OnLocalizationChanged;

    public void NotifyLocalizationChanged()
    {
        OnLocalizationChanged?.Invoke();
    }
}
