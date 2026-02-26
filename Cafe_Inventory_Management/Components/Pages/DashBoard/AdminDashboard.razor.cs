using Cafe_Inventory_Management.Domain;
using Cafe_Inventory_Management.Domain.Model;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;

namespace Cafe_Inventory_Management.UI.Components.Pages.DashBoard
{
    public partial class AdminDashboard : ComponentBase
    {
        private AdminDashboardData _adminData = new();
        private List<StaffResponseDto> _staffPerformance = new();

        private string _staffRange = "7d";
        private bool _isStaffLoading = false;

        protected override async Task OnInitializedAsync()
        {
            // Initial Load
            await LoadAdminStatus();
            await LoadStaffData();
        }

        private async Task LoadAdminStatus()
        {
            var response = await _apiService.APICall(new ApiRequest(HttpMethod.Get, "/GetAdminStatus", "", ""));
            if (response != null && response.ErrorCode == "00")
            {
                _adminData = JsonConvert.DeserializeObject<AdminDashboardData>(response.Detail) ?? new();
            }
        }

        private async Task OnStaffRangeChanged(string newRange)
        {
            _staffRange = newRange;
            await LoadStaffData();
        }

        private async Task LoadStaffData()
        {
            _isStaffLoading = true;
            try
            {
                var response = await _apiService.APICall(new ApiRequest(HttpMethod.Get, $"/GetStaffPerformance?range={_staffRange}", "", ""));
                if (response != null && response.ErrorCode == "00")
                {
                    _staffPerformance = JsonConvert.DeserializeObject<List<StaffResponseDto>>(response.Detail) ?? new();
                }
            }
            finally
            {
                _isStaffLoading = false;
                StateHasChanged();
            }
        }

        private double CalculateProgress(int count)
        {
            if (_staffPerformance == null || !_staffPerformance.Any()) return 0;
            var max = _staffPerformance.Max(x => x.TotalOrdersHandled);
            return max > 0 ? (double)count / max * 100 : 0;
        }
    }

   
}