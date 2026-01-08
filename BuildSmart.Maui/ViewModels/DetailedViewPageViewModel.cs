using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;

namespace BuildSmart.Maui.ViewModels
{
    public partial class DetailedViewPageViewModel : ObservableObject
    {
        private readonly IBuildSmartApiClient _apiClient;

        [ObservableProperty]
        private IGetCurrentUser_CurrentUser? _user;

        public DetailedViewPageViewModel(IBuildSmartApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task LoadUserAsync()
        {
            var result = await _apiClient.GetCurrentUser.ExecuteAsync();
            if (result.Data?.CurrentUser is not null)
            {
                User = result.Data.CurrentUser;
            }
        }
    }
}
