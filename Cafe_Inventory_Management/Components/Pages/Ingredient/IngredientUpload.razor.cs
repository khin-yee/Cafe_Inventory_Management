using ClosedXML.Excel;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Cafe_Inventory_Management.Domain.Model;
using Cafe_Inventory_Management.Domain;
using Cafe_Inventory_Management.UI.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace Cafe_Inventory_Management.UI.Components.Pages.Ingredient
{
    public partial class IngredientUpload:ComponentBase
    {
        private List<Ingredients> _previewData = new();
        private string _sampleExcelDataUrl = "";
        private readonly string _sampleFileName = "Ingredient_Import_Sample.xlsx";
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private HttpClient Http { get; set; }
        [Inject] AuthenticationStateProvider AuthStateProvider { get; set; } = default!;


        [Inject] public IApiCallService _apiService { get; set; }

        private void PrepareSampleExcel()
        {
            try
            {
                using var workbook = new XLWorkbook();
                var sheet = workbook.Worksheets.Add("IngredientImport");

                sheet.Cell(1, 1).Value = "Name";
                sheet.Cell(1, 2).Value = "Code";
                sheet.Cell(1, 3).Value = "Unit";
                sheet.Cell(1, 4).Value = "Quantity";
                sheet.Cell(1, 5).Value = "Amount";

                sheet.Cell(2, 1).Value = "Arabica Beans";
                sheet.Cell(2, 2).Value = "BEAN001";
                sheet.Cell(2, 3).Value = "kg";
                sheet.Cell(2, 4).Value = 5;
                sheet.Cell(2, 5).Value = 30000;

                sheet.Cell(3, 1).Value = "Fresh Milk";
                sheet.Cell(3, 2).Value = "MLK001";
                sheet.Cell(3, 3).Value = "liter";
                sheet.Cell(3, 4).Value = 10;
                sheet.Cell(3, 5).Value = 4500;

                sheet.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                var fileBytes = stream.ToArray();
                var base64 = Convert.ToBase64String(fileBytes);
                _sampleExcelDataUrl = $"data:application/vnd.openxmlformats-officedocument.spreadsheetml.sheet;base64,{base64}";

                Snackbar.Add("Ingredient sample Excel file is ready.", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Failed to prepare sample file: {ex.Message}", Severity.Error);
            }
        }


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
                    _previewData.Add(new Ingredients
                    {
                        Name = row.Cell(1).GetValue<string>(),
                        Code = row.Cell(2).GetValue<string>(),
                        Unit = row.Cell(3).GetValue<string>(),
                        Quantity = row.Cell(4).GetValue<int>(),
                        Amount = row.Cell(5).GetValue<decimal>(),
                        CreatedBy = user.FindFirst(c => c.Type == "name")?.Value?? "Unknown User",
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
            var request = new ApiRequest(HttpMethod.Post, "/BulkUpload", _previewData, "");
            var response = await _apiService.APICall(request);

            if (response.ErrorCode == "00")
            {
                Snackbar.Add("All ingredients saved successfully!", Severity.Success);
                _previewData.Clear();
                Nav.NavigateTo("/Ingredients");

            }
            else
            {
                Snackbar.Add("Failed to save data to database.", Severity.Error);
            }
        }
    }
}
