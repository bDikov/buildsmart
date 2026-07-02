using System;

namespace BuildSmart.SharedUI.Services;

public interface ILocalizationStateService
{
    event Action? OnLocalizationChanged;
    void NotifyLocalizationChanged();
}
