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
    [Inject] IDialogService DialogService { get; set; }


    private bool _isLoading;

    private async Task<TableData<Product>> ServerReload(TableState state, CancellationToken token)
    {
        _isLoading = true; // Start loading state
        StateHasChanged(); // Trigger UI update to show progress bar

        try
        {
            var url = $"/Product?pageNumber={state.Page + 1}&pageSize={state.PageSize}&search={_searchText}";
            var request = new ApiRequest(HttpMethod.Get, url, "", "");

            var response = await _apiService.APICall(request);

            if (response != null && response.ErrorCode == "00")
            {
                var data = JsonConvert.DeserializeObject<PagedResult<Product>>(response.Detail);
                product = data.Items;
                return new TableData<Product>()
                {
                    TotalItems = data.TotalCount ,
                    Items = data.Items
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
    private async Task ConfirmDeleteProduct(int productId)
    {
        var result = await jsRuntime.InvokeAsync<JsonElement>("Swal.fire", new
        {
            title = "Are you sure?",
            text = "Confirm deletion.", 
            icon = "warning",
            width = "300px", 
            height= "100px",
            padding = "1em", 
            showCancelButton = true,
            confirmButtonColor = "#6e7881",
            cancelButtonColor = "#212121", 
            confirmButtonText = "Delete",
            customClass = new
            {
                title = "small-swal-title", 
                content = "small-swal-text"
            }
        });

        if (result.GetProperty("isConfirmed").GetBoolean())
        {
            await DeleteProduct(productId);
        }
    }
    

    private async Task DeleteProduct(int productId)
    {
        try
        {
            var request = new ApiRequest(HttpMethod.Delete, $"/DeleteProduct",productId, "");
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

    private async Task OpenCreateDialog()
    {
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small };
        var parameters = new DialogParameters<ProductDialog>
    {
        { x => x.IsEdit, false },
        { x => x.Model, new Product() }
    };

        var dialog = await DialogService.ShowAsync<ProductDialog>("Add New Product", parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await _table.ReloadServerData();
        }
    }

    private async Task OpenEditDialog(Product productToEdit)
    {
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };

        // Create a copy of the product to avoid modifying the table row before saving
        var parameters = new DialogParameters<ProductDialog>
    {
        { x => x.IsEdit, true },
        { x => x.Model, new Product {
            Name = productToEdit.Name,
            Quatity = productToEdit.Quatity,
            Amount = productToEdit.Amount,
            Category = productToEdit.Category
        }}
    };

        var dialog = await DialogService.ShowAsync<ProductDialog>("Edit Product", parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled && result.Data is Product updatedModel)
        {
            //await UpdateProductInApi(updatedModel);
        }
    }

}