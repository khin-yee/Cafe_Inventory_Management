using Cafe_Inventory_Management.Domain;
using Cafe_Inventory_Management.Domain.Model;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Newtonsoft.Json;

namespace Cafe_Inventory_Management.UI.Components.Pages.DashBoard
{
    public partial class Dashboard : ComponentBase
    {
        private DashboardData _stats = new();
        private List<ChartSeries> _series = new();
        private double[] _pieData = Array.Empty<double>();
        private string[] _pieLabels = Array.Empty<string>();

        private string _selectedRange = "7d";
        private bool _isLoading = true;

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }

        private async Task OnRangeChanged(string range)
        {
            _selectedRange = range;
            await LoadData();
        }

        private async Task LoadData()
        {
            _isLoading = true;
            try
            {
                var response = await _apiService.APICall(new ApiRequest(HttpMethod.Get, $"/GetStatus?range={_selectedRange}", "", ""));

                if (response != null && response.ErrorCode == "00")
                {
                    _stats = JsonConvert.DeserializeObject<DashboardData>(response.Detail) ?? new();

                    _series.Clear();
                    _series.Add(new ChartSeries
                    {
                        Name = "Revenue",
                        Data = _stats.ChartData
                    });

                    if (_stats.CategoryDistribution != null && _stats.CategoryDistribution.Any())
                    {
                        _pieData = _stats.CategoryDistribution.Select(x => x.TotalRevenue).ToArray();
                        _pieLabels = _stats.CategoryDistribution.Select(x => x.CategoryName).ToArray();
                    }
                    else
                    {
                        _pieData = Array.Empty<double>();
                        _pieLabels = Array.Empty<string>();
                    }
                }
            }
            finally
            {
                _isLoading = false;
                StateHasChanged();
            }
        }
    }
}
