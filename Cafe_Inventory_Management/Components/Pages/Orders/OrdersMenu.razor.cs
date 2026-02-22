using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Cafe_Inventory_Management.Domain.Model;
using Cafe_Inventory_Management.Domain;
using Cafe_Inventory_Management.UI.Services;
using Newtonsoft.Json;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Cafe_Inventory_Management.UI.Components.Pages.Orders;
public partial class OrdersMenu : ComponentBase
{
    private List<OrderViewModel> _ordersForUI = new();
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        StateService.OnStockChanged += RefreshList;
        await LoadOrders();
    }


    private async Task OpenEditDialog(OrderViewModel order)
    {
        // We create a copy of the order so we don't change the main list 
        // until the user clicks "Save"
        var parameters = new DialogParameters { ["Order"] = order };
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };

        var dialog = DialogService.Show<EditOrderDialog>("Edit Order #" + order.OrderId, parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await LoadOrders(); // Refresh the list after editing
        }
    }

    private async Task LoadOrders()
    {
        _isLoading = true;
        var url = "/GetOrders"; 
        var request = new ApiRequest(HttpMethod.Get, url, "", "");
        var response = await _apiService.APICall(request);

        if (response != null && response.ErrorCode == "00")
        {
            try
            {
                _ordersForUI = JsonConvert.DeserializeObject<List<OrderViewModel>>(response.Detail);
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }
        _isLoading = false;
    }

    private async void RefreshList()
    {
        await InvokeAsync(async () =>
        {
            await LoadOrders();
            StateHasChanged();
        });
    }

    private async Task UpdateStatus(OrderViewModel order, string newStatus)
    {
        // 1. Logic to call your API to update status in DB
        // Example: await _apiService.UpdateStatus(orderId, newStatus);
        order.Status = newStatus;
        var url = "/UpdateOrderStatus";
        var request = new ApiRequest(HttpMethod.Put, url,order, "");
        var response = await _apiService.APICall(request);
        if (response.ErrorCode == "00")
        {
            // 2. UI Feedback
            Snackbar.Add($"Order #{order} is now {newStatus}", Severity.Success);
        }
        else
        {
            Snackbar.Add($"Order status change error for  {newStatus}", Severity.Error);

        }
        StateService.NotifyStockChanged();

        await LoadOrders();
    }

    private Color GetStatusColor(string status) => status switch
    {
        "Pending" => Color.Warning,
        "Preparing" => Color.Info,
        "Completed" => Color.Success,
        _ => Color.Default
    };

    public void Dispose()
    {
        StateService.OnStockChanged -= RefreshList;
    }
}

