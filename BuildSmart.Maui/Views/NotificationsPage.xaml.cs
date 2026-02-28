using BuildSmart.Maui.ViewModels;

namespace BuildSmart.Maui.Views;

public partial class NotificationsPage : ContentPage
{
	public NotificationsPage(NotificationsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (BindingContext is NotificationsViewModel vm)
		{
			await vm.LoadNotificationsAsync();
		}
	}

    private async void OnClearAllClicked(object sender, EventArgs e)
    {
        if (BindingContext is not NotificationsViewModel vm || vm.Notifications.Count == 0)
            return;

        // ANIMATION: Bottom to Top
        // We iterate backwards through the Children of the StackLayout
        var children = NotificationsList.Children.ToList();
        
        for (int i = children.Count - 1; i >= 0; i--)
        {
            var child = children[i] as VisualElement;
            if (child == null) continue;

            // Run slide-out animation
            // Using a Task.Run or just firing and forgetting the animation 
            // so we don't wait for EACH one to finish before starting the next.
            // But we add a small delay to create the staggered "ripple" effect.
            AnimateItemOut(child);
            await Task.Delay(100); 
        }

        // Final delay to ensure last item is mostly gone before wipe
        await Task.Delay(250);

        // Actual deletion
        await vm.DeleteAllAsync();
    }

    private async void AnimateItemOut(VisualElement view)
    {
        // Slide to the right (Width of screen) and Fade to zero
        uint duration = 400;
        await Task.WhenAll(
            view.TranslateTo(view.Width + 100, 0, duration, Easing.CubicIn),
            view.FadeTo(0, duration, Easing.CubicIn)
        );
    }
}
