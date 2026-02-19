using Cafe_Inventory_Management.Domain.Model;
using Cafe_Inventory_Management.Domain;
using Cafe_Inventory_Management.UI.Services;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Cafe_Inventory_Management.UI.Components.Pages.Products;
public partial class ProductUpload : ComponentBase
{
    private List<Product> _previewData = new();
    [Inject] private ISnackbar Snackbar { get; set; }
    [Inject] private HttpClient Http { get; set; }
    [Inject] AuthenticationStateProvider AuthStateProvider { get; set; } = default!;


    [Inject] public IApiCallService _apiService { get; set; }


    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file == null) return;

        try
        {
            using var stream = new MemoryStream();
            await file.OpenReadStream().CopyToAsync(stream);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);
            var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Skip Header

            _previewData.Clear();
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            foreach (var row in rows)
            {
                _previewData.Add(new Product
                {
                    Name = row.Cell(1).GetValue<string>(),
                    Code = row.Cell(2).GetValue<string>(),
                    Category = row.Cell(3).GetValue<string>(),
                    Quatity = row.Cell(4).GetValue<int>(),
                    Amount = row.Cell(5).GetValue<decimal>(),
                    CreatedBy = user.FindFirst(c => c.Type == "name")?.Value?? "Unknown User",
                    IsRecipe = row.Cell(6).GetValue<bool>(),
                    IsActive = true
                });
            }
            Snackbar.Add("File parsed! Please review the data below.", Severity.Info);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
    }

    private async Task SubmitToDatabase()
    {
        var request = new ApiRequest(HttpMethod.Post, "/ProductBulkUpload", _previewData, "");
        var response = await _apiService.APICall(request);

        if (response.ErrorCode == "00")
        {
            Snackbar.Add("All ingredients saved successfully!", Severity.Success);
            _previewData.Clear();
        }
        else
        {
            Snackbar.Add("Failed to save data to database.", Severity.Error);
        }
    }
}

