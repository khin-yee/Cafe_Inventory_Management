using Cafe_Inventory_Management.Domain.Model;
using Cafe_Inventory_Management.Domain;
using Cafe_Inventory_Management.UI.Services;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using Newtonsoft.Json;

namespace Cafe_Inventory_Management.UI.Components.Pages.Products;
public partial class ProductUpload : ComponentBase
{
    private List<ProductRequest> _previewData = new();
    private string _sampleExcelDataUrl = "";
    private readonly string _sampleFileName = "Product_Import_Sample.xlsx";
    [Inject] private ISnackbar Snackbar { get; set; }
    [Inject] private HttpClient Http { get; set; }
    [Inject] AuthenticationStateProvider AuthStateProvider { get; set; } = default!;


    [Inject] public IApiCallService _apiService { get; set; }

    private void PrepareSampleExcel()
    {
        try
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("ProductImport");

            sheet.Cell(1, 1).Value = "Name";
            sheet.Cell(1, 2).Value = "Code";
            sheet.Cell(1, 3).Value = "Category";
            sheet.Cell(1, 4).Value = "Quantity";
            sheet.Cell(1, 5).Value = "Amount";
            sheet.Cell(1, 6).Value = "IsRecipe";
            sheet.Cell(1, 7).Value = "IngredientCode";
            sheet.Cell(1, 8).Value = "RequiredAmount";

            sheet.Cell(2, 1).Value = "Latte";
            sheet.Cell(2, 2).Value = "LATTE001";
            sheet.Cell(2, 3).Value = "Coffee";
            sheet.Cell(2, 4).Value = 10;
            sheet.Cell(2, 5).Value = 4500;
            sheet.Cell(2, 6).Value = true;
            sheet.Cell(2, 7).Value = "ESP001";
            sheet.Cell(2, 8).Value = 1;

            sheet.Cell(3, 1).Value = "Latte";
            sheet.Cell(3, 2).Value = "LATTE001";
            sheet.Cell(3, 3).Value = "Coffee";
            sheet.Cell(3, 4).Value = 10;
            sheet.Cell(3, 5).Value = 4500;
            sheet.Cell(3, 6).Value = true;
            sheet.Cell(3, 7).Value = "MLK001";
            sheet.Cell(3, 8).Value = 0.2;

            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileBytes = stream.ToArray();
            var base64 = Convert.ToBase64String(fileBytes);
            _sampleExcelDataUrl = $"data:application/vnd.openxmlformats-officedocument.spreadsheetml.sheet;base64,{base64}";

            Snackbar.Add("Sample Excel file is ready.", Severity.Success);
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
            var requestByProductCode = new Dictionary<string, ProductRequest>(StringComparer.OrdinalIgnoreCase);
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            foreach (var row in rows)
            {
                var productCode = row.Cell(2).GetValue<string>()?.Trim();
                if (string.IsNullOrWhiteSpace(productCode))
                {
                    continue;
                }

                if (!requestByProductCode.TryGetValue(productCode, out var importRequest))
                {
                    importRequest = new ProductRequest
                    {
                        Product = new Product
                        {
                            Name = row.Cell(1).GetValue<string>(),
                            Code = productCode,
                            Category = row.Cell(3).GetValue<string>(),
                            Quatity = row.Cell(4).GetValue<int>(),
                            Amount = row.Cell(5).GetValue<decimal>(),
                            CreatedBy = user.FindFirst(c => c.Type == "name")?.Value ?? "Unknown User",
                            IsRecipe = row.Cell(6).GetValue<bool>(),
                            IsActive = true
                        },
                        Recipe = new List<ProductIngredients>()
                    };

                    requestByProductCode[productCode] = importRequest;
                }

                var ingredientCode = row.Cell(7).GetValue<string>()?.Trim();
                var requiredAmount = row.Cell(8).GetValue<decimal>();
                if (!string.IsNullOrWhiteSpace(ingredientCode) && requiredAmount > 0)
                {
                    importRequest.Recipe.Add(new ProductIngredients
                    {
                        ProductCode = productCode,
                        IngredientCode = ingredientCode,
                        RequiredAmount = requiredAmount,
                        IsActive = true,
                        CreatedBy = user.FindFirst(c => c.Type == "name")?.Value ?? "Unknown User"
                    });
                }
            }

            _previewData = requestByProductCode.Values.ToList();

            if (!_previewData.Any())
            {
                Snackbar.Add("No valid rows were found in the selected file.", Severity.Warning);
                return;
            }

            if (_previewData.Any(x => x.Recipe == null || x.Recipe.Count == 0))
            {
                Snackbar.Add("Some products do not include ingredient rows. Please add IngredientCode and RequiredAmount columns.", Severity.Warning);
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
            var apiResult = JsonConvert.DeserializeObject<ApiResponse>(response.Detail ?? "{}");
            Snackbar.Add(GetUiSuccessMessage(apiResult?.ErrorMessage, "All products and ingredients saved successfully!"), Severity.Success);
            _previewData.Clear();
        }
        else
        {
            var message = "Failed to save data to database.";
            if (!string.IsNullOrWhiteSpace(response.Detail))
            {
                try
                {
                    var apiError = JsonConvert.DeserializeObject<ApiResponse>(response.Detail);
                    if (!string.IsNullOrWhiteSpace(apiError?.ErrorMessage))
                    {
                        message = apiError.ErrorMessage;
                    }
                }
                catch
                {
                }
            }

            Snackbar.Add(message, Severity.Error);
        }
    }

    private static string GetUiSuccessMessage(string? apiMessage, string fallback)
    {
        if (string.IsNullOrWhiteSpace(apiMessage) || string.Equals(apiMessage.Trim(), "No Error", StringComparison.OrdinalIgnoreCase))
        {
            return fallback;
        }

        return apiMessage;
    }
}

