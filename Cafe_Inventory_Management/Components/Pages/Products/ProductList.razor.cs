using Cafe_Inventory_Management.Domain;
using Cafe_Inventory_Management.Domain.Model;
using Cafe_Inventory_Management.UI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Newtonsoft.Json;
using System.Text.Json;

namespace Cafe_Inventory_Management.UI.Components.Pages.Products;
public partial class ProductList : ComponentBase
{
    private MudTable<Product> _table;
    private List<Product>? product;
    private string _searchText = "";
    private List<Product>? _allProducts; // Original list

    private bool _isInitialized = false;
    [Inject] public IApiCallService _apiService { get; set; }
    [Inject] IJSRuntime jsRuntime { get; set; }
    [Inject] NavigationManager navigationManager { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // Load or reload data every time the page is visited
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var request = new ApiRequest(HttpMethod.Get, "/Product", "", "");
            var response = await _apiService.APICall(request);

            if (response.ErrorCode == "00")
            {
                _allProducts = JsonConvert.DeserializeObject<List<Product>>(response.Detail);

                // Default = show all
                product = _allProducts;
            }

            _isInitialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching products: {ex.Message}");
        }
    }

    private void SearchProduct()
    {
        if (string.IsNullOrWhiteSpace(_searchText))
        {
            // Reset to all products
            product = _allProducts;
        }
        else
        {
            product = _allProducts
                ?.Where(p => p.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // Reset pagination
        _table?.NavigateTo(0);
    }

    private void PageChanged(int i)
    {
        _table.NavigateTo(i - 1);
    }



    private async Task ConfirmDeleteProduct(string productName)
    {
        try
        {
            // Show confirmation dialog
            var result = await jsRuntime.InvokeAsync<JsonElement>("Swal.fire", new
            {
                title = "Are you sure?",
                text = "This action cannot be undone!",
                icon = "warning",
                showCancelButton = true,
                confirmButtonColor = "grey",
                cancelButtonColor = "dark",
                confirmButtonText = "Delete",
                cancelButtonText = "Cancel"
            });
            Console.WriteLine(result);
            // Check if the user confirmed the deletion
            bool isConfirmed = result.GetProperty("isConfirmed").GetBoolean();
            bool isDenied = result.GetProperty("isDenied").GetBoolean();
            bool isDismissed = result.GetProperty("isDismissed").GetBoolean(); if (isConfirmed)
            {
                await DeleteProduct(productName);
            }
            else
            {
                Console.WriteLine("User canceled the delete action.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ConfirmDeleteProduct: {ex.Message}");
        }
    }

    private async Task DeleteProduct(string productName)
    {
        // Add your logic to delete the product using the productName
        // For instance, call the API to delete the product and refresh the product list afterward.
        try
        {
            var request = new ApiRequest(HttpMethod.Get, "https://localhost:7055/Product", "", "");
            var response = await _apiService.APICall(request);
            // Optionally, refresh the product list
            if (response.ErrorCode == "00")
            {
                await jsRuntime.InvokeVoidAsync("Swal.fire", new
                {
                    title = "Success!",
                    text = "Product Deleted successfully!",
                    icon = "success",
                    confirmButtonText = "OK"
                });
            }
            else
            {
                await jsRuntime.InvokeVoidAsync("Swal.fire", new
                {
                    title = "Error!",
                    text = response.ErrorMessage,
                    icon = "error",
                    confirmButtonText = "OK",
                    confirmButtonColor = "dark",

                });
            }
            navigationManager.NavigateTo("/Product", true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting product: {ex.Message}");
        }
    }

}

