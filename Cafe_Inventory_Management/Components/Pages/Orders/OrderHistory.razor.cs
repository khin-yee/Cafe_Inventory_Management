using Cafe_Inventory_Management.Domain.Model;
using Cafe_Inventory_Management.Domain;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using MudBlazor;
using Microsoft.JSInterop;
using ClosedXML.Excel;
using System.Data;
using Cafe_Inventory_Management.UI.Components.Pages.Products;

namespace Cafe_Inventory_Management.UI.Components.Pages.Orders;
public partial class OrderHistory:ComponentBase
{
    private MudTable<OrderViewModel> _table;
    private string _searchString = "";
    [Inject] IDialogService DialogService { get; set; }

    private DateRange _dateRange = new DateRange(DateTime.Now.Date.AddDays(-7), DateTime.Now.Date);

    private string _excelDataUrl = "";
    private string _fileName = "";
    private bool _isProcessing = false;
    private bool _isEmailing = false;

    // This method is called ONLY when the button is clicked
    private async Task PerformSearch()
    {
        await _table.ReloadServerData();
    }

    private async Task<TableData<OrderViewModel>> ServerReload(TableState state,CancellationToken token)
    {
        try
        {
            var start = _dateRange.Start?.ToString("yyyy-MM-dd");
            var end = _dateRange.End?.ToString("yyyy-MM-dd");

            // page and pageSize are still handled by MudTable's pager
            var url = $"/GetOrderSummary?page={state.Page}&pageSize={state.PageSize}&search={_searchString}&start={start}&end={end}";

            var response = await _apiService.APICall(new ApiRequest(HttpMethod.Get, url, "", ""));

            if (response != null && response.ErrorCode == "00")
            {
                var pagedData = JsonConvert.DeserializeObject<PagedResult<OrderViewModel>>(response.Detail);

                return new TableData<OrderViewModel>()
                {
                    TotalItems = pagedData.TotalCount,
                    Items = pagedData.Items
                };
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add("Error: " + ex.Message, Severity.Error);
        }

        return new TableData<OrderViewModel>() { TotalItems = 0, Items = new List<OrderViewModel>() };
    }

    private async Task ViewDetails(OrderViewModel order)
    {
        // Calling the API to get the full model including the Items list
        var response = await _apiService.APICall(new ApiRequest(HttpMethod.Get, $"/GetOrderDetails/{order.OrderId}", "", ""));

        if (response != null && response.ErrorCode == "00")
        {
            // Deserialize into your existing OrderViewModel
            var detailedOrder = JsonConvert.DeserializeObject<OrderViewModel>(response.Detail);

            var parameters = new DialogParameters { ["Order"] = detailedOrder };
            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };

            var dialog = await DialogService.ShowAsync<OrderDetailDialog>("Order Details", parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                await _table.ReloadServerData();
            }
        }
    }
    private async Task PrepareExcel()
    {
        _isProcessing = true;
        _excelDataUrl = ""; // Reset previous link

        try
        {
            var start = _dateRange.Start?.ToString("yyyy-MM-dd");
            var end = _dateRange.End?.ToString("yyyy-MM-dd");
            var url = $"https://localhost:7223/ExportExcel?search={_searchString}&start={start}&end={end}";

            // 1. Use a clean HttpClient to fetch binary data directly
            var client = new HttpClient();
            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                // 2. Read as Byte Array (this prevents file corruption)
                byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();

                // 3. Convert to Base64 for the No-JS download link
                var base64 = Convert.ToBase64String(fileBytes);
                _excelDataUrl = $"data:application/vnd.openxmlformats-officedocument.spreadsheetml.sheet;base64,{base64}";
                _fileName = $"OrderReport_{DateTime.Now:yyyyMMdd}.xlsx";

                Snackbar.Add("Excel file ready for download!", Severity.Success);
            }
            else
            {
                Snackbar.Add("Server error generating file.", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add("Export error: " + ex.Message, Severity.Error);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private async Task SendManualEmailToAdmin(bool montly)
    {
        _isEmailing = true;
        try
        {
            //var payload = new
            //{
            //    //StartDate = _dateRange.Start,
            //    //EndDate = _dateRange.End,
            //    IsMonthly = montly 
            //};

            var response = await _apiService.APICall(new ApiRequest(
                HttpMethod.Post,
                "/EmailToAdmin",
                montly,
                "")
            );

            if (response != null && response.ErrorCode == "00")
            {
                Snackbar.Add("Report emailed to administrators successfully.", Severity.Success);
            }
            else
            {
                Snackbar.Add("Failed to send email. Ensure SMTP settings are correct.", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isEmailing = false;
        }
    }
}

