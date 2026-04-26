using BuildSmart.SharedUI.ViewModels;

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

        // Since we migrated to a virtualized CollectionView for performance,
        // we animate the entire list out instead of individual visual children.
        await Task.WhenAll(
            NotificationsList.TranslateTo(NotificationsList.Width + 100, 0, 400, Easing.CubicIn),
            NotificationsList.FadeTo(0, 400, Easing.CubicIn)
        );

        // Actual deletion
        await vm.DeleteAllAsync();

        // Reset the CollectionView properties for future use
        NotificationsList.TranslationX = 0;
        NotificationsList.Opacity = 1;
    }
}

