using Cafe_Inventory_Management.Domain;
using Cafe_Inventory_Management.Domain.Model;
using Cafe_Inventory_Management.UI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using Newtonsoft.Json;
using System.Text.Json;

namespace Cafe_Inventory_Management.UI.Components.Pages.Products;

public partial class ProductList : ComponentBase
{
    private MudTable<Product> _table;
    private string _searchText = "";
    private bool _showSearch = false;
    private bool _isInitialized = false;
    private List<Product> product = new();
    [Inject] public IApiCallService _apiService { get; set; }
    [Inject] IJSRuntime jsRuntime { get; set; }
    [Inject] NavigationManager navigationManager { get; set; }

    /// <summary>
    /// This method is called by MudTable whenever it needs data (on load, page change, or sort).
    /// </summary>
    // In your ProductList.razor.cs

    private bool _isLoading;

    private async Task<TableData<Product>> ServerReload(TableState state, CancellationToken token)
    {
        _isLoading = true; // Start loading state
        StateHasChanged(); // Trigger UI update to show progress bar

        try
        {
            // 1. Prepare pagination and search params
            var query = $"?pageNumber={state.Page + 1}&pageSize={state.PageSize}&searchTerm={_searchText}";

            // 2. Pass the 'token' to your API service if it supports it
            var request = new ApiRequest(HttpMethod.Get, $"/Product", "", "");

            // Simulating the call - make sure your APICall method is as lean as possible
            var response = await _apiService.APICall(request);

            if (response != null && response.ErrorCode == "00")
            {
                var data = JsonConvert.DeserializeObject<List<Product>>(response.Detail);
                product = data;
                return new TableData<Product>()
                {
                    TotalItems = data.Count, // API should ideally return total count separately
                    Items = data
                };
            }
        }
        catch (TaskCanceledException)
        {
            // Ignore cancellations when user types fast
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fetch error: {ex.Message}");
        }
        finally
        {
            _isLoading = false; // Stop loading state
            StateHasChanged();
        }

        return new TableData<Product>() { TotalItems = 0, Items = new List<Product>() };
    }
    private void SearchProduct()
    {
        // Calling ReloadServerData triggers the ServerReload method automatically
        _table.ReloadServerData();
    }

    private void HandleKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            SearchProduct();
        }
    }

    private async Task ConfirmDeleteProduct(string productName)
    {
        var result = await jsRuntime.InvokeAsync<JsonElement>("Swal.fire", new
        {
            title = "Are you sure?",
            text = "This action cannot be undone!",
            icon = "warning",
            showCancelButton = true,
            confirmButtonColor = "grey",
            cancelButtonColor = "dark",
            confirmButtonText = "Delete"
        });

        if (result.GetProperty("isConfirmed").GetBoolean())
        {
            await DeleteProduct(productName);
        }
    }

    private async Task DeleteProduct(string productName)
    {
        try
        {
            var request = new ApiRequest(HttpMethod.Delete, $"/Product/{productName}", "", "");
            var response = await _apiService.APICall(request);

            if (response.ErrorCode == "00")
            {
                await jsRuntime.InvokeVoidAsync("Swal.fire", "Success", "Product Deleted", "success");
                await _table.ReloadServerData(); // Refresh table after delete
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting product: {ex.Message}");
        }
    }
}