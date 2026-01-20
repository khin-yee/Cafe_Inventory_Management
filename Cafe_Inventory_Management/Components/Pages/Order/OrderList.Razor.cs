using Cafe_Inventory_Management.Domain;
using Cafe_Inventory_Management.UI.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Newtonsoft.Json;
using Cafe_Inventory_Management.UI.Services;

namespace Cafe_Inventory_Management.UI.Components.Pages.Order;

public partial class OrderList : ComponentBase
{

    public List<OrderResponse> Orders = new();
    public  bool _loading = true;
    private string _searchString = "";

    protected override async Task OnInitializedAsync()
    {
        await LoadOrders();
    }

    private async Task LoadOrders()
    {
        try
        {
            _loading = true;
            // Assuming your base URL is in config as seen in your appsettings.json
            var url = "https://localhost:7055/api/orders";
            var request = new ApiRequest(HttpMethod.Get, url, null);

            var response = await _apiService.APICall(request);

            if (response != null && response.ErrorCode == "00")
            {
                Orders = JsonConvert.DeserializeObject<List<OrderResponse>>(response.Detail!) ?? new();
            }
            else
            {
            }
        }
        catch (Exception)
        {
        }
        finally
        {
            _loading = false;
        }
    }

    private bool FilterFunc1(OrderResponse element) => FilterFunc(element, _searchString);

    private bool FilterFunc(OrderResponse element, string searchString)
    {
        if (string.IsNullOrWhiteSpace(searchString)) return true;
        if (element.CustomerName.Contains(searchString, StringComparison.OrdinalIgnoreCase)) return true;
        if (element.OrderId.ToString().Contains(searchString)) return true;
        return false;
    }

    public Color GetStatusColor(string status)
    {
        return status.ToLower() switch
        {
            "completed" => Color.Success,
            "pending" => Color.Warning,
            "cancelled" => Color.Error,
            _ => Color.Default
        };
    }


}