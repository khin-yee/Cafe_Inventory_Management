using Cafe_Inventory_Management.Domain.Model;
using Cafe_Inventory_Management.Domain;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using MudBlazor;

namespace Cafe_Inventory_Management.UI.Components.Pages.Orders;
public partial class OrderHistory:ComponentBase
{
    private MudTable<OrderViewModel> _table;
    private string _searchString = "";
    private DateRange _dateRange = new DateRange(DateTime.Now.Date.AddDays(-7), DateTime.Now.Date);

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

    private void ViewDetails(OrderViewModel order) { /* Logic */ }
}

