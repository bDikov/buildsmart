using BuildSmart.SharedUI.Services;
using BuildSmart.SharedUI.MauiMocks;
using BuildSmart.SharedUI.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BuildSmart.SharedUI.ViewModels.Admin;

[QueryProperty(nameof(CategoryId), "categoryId")]
public partial class AdminCategorySkusViewModel : ObservableObject, IQueryAttributable
{
    private readonly IBuildSmartApiClient _apiClient;

    public AdminCategorySkusViewModel(IBuildSmartApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [ObservableProperty]
    private string? _categoryId;

    public ObservableCollection<IGetCategorySkus_ServiceSkusByCategory> Skus { get; } = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isCreatingSku;

    [ObservableProperty]
    private Guid? _editingSkuId;

    [ObservableProperty]
    private string _newSkuCode = string.Empty;
    
    [ObservableProperty]
    private string _newSkuName = string.Empty;
    
    [ObservableProperty]
    private string _newSkuDescription = string.Empty;
    
    [ObservableProperty]
    private string _newSkuBasePrice = string.Empty;
    
    [ObservableProperty]
    private string _newSkuUnitType = "Per Quantity (Item)";

    public List<string> AvailableUnitTypes { get; } = new()
    {
        "Per Quantity (Item)",
        "Per Square Meter",
        "Per Linear Meter",
        "Flat Rate",
        "Hourly"
    };

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("categoryId", out var idObj) && idObj != null)
        {
            CategoryId = idObj.ToString();
            AppServiceLocator.MainThread.BeginInvokeOnMainThread(async () => await LoadSkusAsync());
        }
    }

    [RelayCommand]
    public async Task LoadSkusAsync()
    {
        if (string.IsNullOrEmpty(CategoryId) || !Guid.TryParse(CategoryId, out var guidId)) return;
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            var result = await _apiClient.GetCategorySkus.ExecuteAsync(guidId);

            if (result.Errors.Count > 0)
            {
                await AppServiceLocator.Alerts.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }

            Skus.Clear();
            if (result.Data?.ServiceSkusByCategory != null)
            {
                foreach (var sku in result.Data.ServiceSkusByCategory)
                {
                    Skus.Add(sku);
                }
            }
        }
        catch (Exception ex)
        {
            await AppServiceLocator.Alerts.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void ShowCreateSkuForm()
    {
        EditingSkuId = null;
        NewSkuCode = string.Empty;
        NewSkuName = string.Empty;
        NewSkuDescription = string.Empty;
        NewSkuBasePrice = "0";
        NewSkuUnitType = AvailableUnitTypes.First();

        IsCreatingSku = true;
    }

    [RelayCommand]
    public void EditSku(IGetCategorySkus_ServiceSkusByCategory sku)
    {
        EditingSkuId = sku.Id;
        NewSkuCode = sku.SkuCode;
        NewSkuName = sku.Name;
        NewSkuDescription = sku.Description ?? string.Empty;
        NewSkuBasePrice = sku.BasePrice.ToString();
        NewSkuUnitType = sku.UnitType;

        IsCreatingSku = true;
    }

    [RelayCommand]
    public void CancelCreateSku()
    {
        IsCreatingSku = false;
        EditingSkuId = null;
    }

    [RelayCommand]
    public async Task SaveNewSkuAsync()
    {
        if (string.IsNullOrWhiteSpace(NewSkuCode) || string.IsNullOrWhiteSpace(NewSkuName))
        {
            await AppServiceLocator.Alerts.DisplayAlert("Validation Error", "SKU Code and Name are required.", "OK");
            return;
        }

        if (!decimal.TryParse(NewSkuBasePrice, out var price))
        {
            await AppServiceLocator.Alerts.DisplayAlert("Validation Error", "Base Price must be a valid number.", "OK");
            return;
        }

        try
        {
            IsBusy = true;
            
            if (EditingSkuId.HasValue)
            {
                var result = await _apiClient.UpdateServiceSku.ExecuteAsync(EditingSkuId.Value, NewSkuCode, NewSkuName, NewSkuDescription ?? string.Empty, price, NewSkuUnitType);
                if (result.Errors.Count > 0)
                {
                    await AppServiceLocator.Alerts.DisplayAlert("Error", result.Errors[0].Message, "OK");
                    return;
                }
            }
            else
            {
                var result = await _apiClient.CreateServiceSku.ExecuteAsync(Guid.Parse(CategoryId!), NewSkuCode, NewSkuName, NewSkuDescription ?? string.Empty, price, NewSkuUnitType);
                if (result.Errors.Count > 0)
                {
                    await AppServiceLocator.Alerts.DisplayAlert("Error", result.Errors[0].Message, "OK");
                    return;
                }
            }

            IsCreatingSku = false;
            EditingSkuId = null;
            await LoadSkusAsync(); // Reload the list
        }
        catch (Exception ex)
        {
            await AppServiceLocator.Alerts.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task DeleteSkuAsync(Guid skuId)
    {
        bool confirm = await AppServiceLocator.Alerts.DisplayAlert("Confirm", "Delete this SKU?", "Yes", "No");
        if (!confirm) return;

        try
        {
            IsBusy = true;
            var result = await _apiClient.DeleteServiceSku.ExecuteAsync(skuId);

            if (result.Errors.Count > 0)
            {
                await AppServiceLocator.Alerts.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }

            await LoadSkusAsync();
        }
        catch (Exception ex)
        {
            await AppServiceLocator.Alerts.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}




