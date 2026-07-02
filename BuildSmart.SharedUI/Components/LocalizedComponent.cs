using System;
using Microsoft.AspNetCore.Components;
using BuildSmart.SharedUI.Services;

namespace BuildSmart.SharedUI.Components;

public class LocalizedComponent : ComponentBase, IDisposable
{
    [Inject]
    protected ILocalizationStateService StateService { get; set; } = null!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        StateService.OnLocalizationChanged += HandleLocalizationChanged;
    }

    private void HandleLocalizationChanged()
    {
        InvokeAsync(StateHasChanged);
    }

    public virtual void Dispose()
    {
        StateService.OnLocalizationChanged -= HandleLocalizationChanged;
    }
}
