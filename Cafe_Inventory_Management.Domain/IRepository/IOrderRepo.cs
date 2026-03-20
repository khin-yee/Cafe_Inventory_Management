using Cafe_Inventory_Management.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cafe_Inventory_Management.Domain.IRepository;
public interface IOrderRepo
{
    Task<ApiResponse> SaveOrder(OrderRequestDto request);
    Task<ApiResponse> UpdateOrder(OrderViewModel updatedOrder);
    Task<List<OrderViewModel>> GetOrders();
    Task<ApiResponse> UpdateOrderStatus(OrderViewModel updatedOrder);
    Task<PagedResult<OrderViewModel>> GetAllSuccessOrders(int page, int pageSize, string? search, DateTime? start, DateTime? end);
    Task<List<OrderViewModel>> ExportHistory(string? search, DateTime? start, DateTime? end);
    Task<DashboardData> GetStats(string range);
    Task<List<StaffResponseDto>> GetStaffPerformance(string range);
    Task<AdminDashboardData> GetAdminStatus();
    Task<OrderViewModel> GetOrderDetails(string orderId);

    Task<List<OrderViewModel>> GetOrdersByDate(DateTime startDate, DateTime endDate);


}

