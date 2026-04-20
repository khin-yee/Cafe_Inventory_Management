using Cafe_Inventory_Management.Domain.Model;
using Cafe_Inventory_Management.Domain;
using Cafe_Inventory_Management.UI.Components.Pages.Ingredient;
using Cafe_Inventory_Management.UI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Components.Forms;

namespace Cafe_Inventory_Management.UI.Components.Pages.Ingredient;
public partial class IngredientsList : ComponentBase
{
    private MudTable<Ingredients> _table;
    private string _searchText = "";
    private bool _showSearch = false;
    private bool _isInitialized = false;
    private List<Ingredients> Ingredients = new();
    [Inject] public IApiCallService _apiService { get; set; }
    [Inject] IJSRuntime jsRuntime { get; set; }
    [Inject] NavigationManager navigationManager { get; set; }
    [Inject] IDialogService DialogService { get; set; }
    [Inject] private ISnackbar Snackbar { get; set; }

    private bool _isLoading;

    private async Task<TableData<Ingredients>> ServerReload(TableState state, CancellationToken token)
    {
        _isLoading = true; // Start loading state
        StateHasChanged(); // Trigger UI update to show progress bar

        try
        {
            var url = $"/Ingredients?pageNumber={state.Page + 1}&pageSize={state.PageSize}&search={_searchText}";
            var request = new ApiRequest(HttpMethod.Get, url, "", "");

            var response = await _apiService.APICall(request);

            if (response != null && response.ErrorCode == "00")
            {
                var data = JsonConvert.DeserializeObject<PagedResult<Ingredients>>(response.Detail);
                Ingredients = data.Items;
                return new TableData<Ingredients>()
                {
                    TotalItems = data.TotalCount,
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

        return new TableData<Ingredients>() { TotalItems = 0, Items = new List<Ingredients>() };
    }
    private void SearchIngredients()
    {
        // Calling ReloadServerData triggers the ServerReload method automatically
        _table.ReloadServerData();
    }

    private void HandleKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            SearchIngredients();
        }
    }
    private async Task ConfirmDeleteIngredients(int IngredientsId)
    {
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraSmall,
            FullWidth = true,
            CloseButton = true,

        };
        bool? result = await DialogService.ShowMessageBox(
         new MessageBoxOptions
         {
             Title = "Warning",
             MarkupMessage = new MarkupString("Are you sure you want to delete this ingredient?<br/><b>This action cannot be undone.</b>"),
             YesText = "Delete",
             CancelText = "Cancel",
         },
         options
     );

        if (result == true)
        {
            await DeleteIngredients(IngredientsId);
        }
    }


    private async Task DeleteIngredients(int IngredientsId)
    {
        try
        {
            var request = new ApiRequest(HttpMethod.Delete, $"/DeleteIngredients", IngredientsId, "");
            var response = await _apiService.APICall(request);

            if (response.ErrorCode == "00")
            {
                Snackbar.Add("Ingredient deleted successfully.", Severity.Success);
                await _table.ReloadServerData();
            }
            else
            {
                Snackbar.Add($"Unable to delete ingredient: {response.ErrorMessage}", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Unable to delete ingredient: {ex.Message}", Severity.Error);
        }
    }

    private async Task OpenCreateDialog()
    {
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small };
        var parameters = new DialogParameters<IngredientsDialog>
    {
        { x => x.IsEdit, false },
        { x => x.Model, new Ingredients() }
    };

        var dialog = await DialogService.ShowAsync<IngredientsDialog>("Add New Ingredients", parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await _table.ReloadServerData();
        }
    }

    private async Task OpenEditDialog(Ingredients IngredientsToEdit)
    {
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };

        // Create a copy of the Ingredients to avoid modifying the table row before saving
        var parameters = new DialogParameters<IngredientsDialog>
    {
        { x => x.IsEdit, true },
        { x => x.Model, IngredientsToEdit}
    };

        var dialog = await DialogService.ShowAsync<IngredientsDialog>("Edit Ingredients", parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled && result.Data is Ingredients updatedModel)
        {
            //await UpdateIngredientsInApi(updatedModel);
        }
    }


    private async Task UploadExcel(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file == null) return;

        try
        {
            using var stream = new MemoryStream();
            await file.OpenReadStream().CopyToAsync(stream);

            using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1); // Read the first sheet
            var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Skip Header row

            var ingredientsToUpload = new List<Ingredients>();

            foreach (var row in rows)
            {
                ingredientsToUpload.Add(new Ingredients
                {
                    Name = row.Cell(1).GetValue<string>(),
                    Code = row.Cell(2).GetValue<string>(),
                    Quantity = row.Cell(3).GetValue<int>(),
                    Unit = row.Cell(4).GetValue<string>(),
                    IsActive = true
                });
            }

            var request = new ApiRequest(HttpMethod.Post, "/Ingredients/BulkUpload", ingredientsToUpload, "");
            var response = await _apiService.APICall(request);

            if (response.ErrorCode == "00")
            {
                Snackbar.Add("Ingredient import completed successfully.", Severity.Success);
                await _table.ReloadServerData();
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error parsing Excel: {ex.Message}", Severity.Error);
        }
    }

}
