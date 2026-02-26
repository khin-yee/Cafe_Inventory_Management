using Cafe_Inventory_Management.Domain;
using Cafe_Inventory_Management.Domain.IRepository;
using Cafe_Inventory_Management.Domain.IServices;
using Cafe_Inventory_Management.Domain.Model;
using ClosedXML.Excel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Cafe_Inventory_Management.Service;
public class OrderService:IOrderService
{
    public readonly IOrderRepo _repo;

    public OrderService(IOrderRepo repo)
    {
        _repo = repo;
    }

    public async Task<ApiResponse> CreateOrder(OrderRequestDto order)
    {
         return await _repo.SaveOrder(order);     
    }

    public async Task<ApiResponse> UpdateOrder(OrderViewModel updatedOrder)
    {
        return  await _repo.UpdateOrder(updatedOrder);
       
    }

    public async Task<List<OrderViewModel>> GetOrders()
    {
        var result = await _repo.GetOrders();
    
        return result;
    }

    public async Task<ApiResponse> UpdateOrderStatus(OrderViewModel updatedOrder)
    {
        var result = await _repo.UpdateOrderStatus(updatedOrder);
        return result;
    }

    public async Task<PagedResult<OrderViewModel>> GetAllSuccessOrders(int page, int pageSize, string? search, DateTime? start, DateTime? end)
    {
        var result = await _repo.GetAllSuccessOrders(page,pageSize,search,start,end);
        return result;
    }
    public async Task<List<OrderViewModel>> ExcelExport(string? search, DateTime? start, DateTime? end)
    {
        var data = await _repo.ExportHistory(search, start, end);
        return data;

    }

    public async Task<DashboardData> GetStats(string range)
    {
        return await _repo.GetStats(range);
    }

    public async Task<List<StaffResponseDto>> GetStaffPerformance(string range)
    {
        return await _repo.GetStaffPerformance(range);
    }

    public async Task<AdminDashboardData> GetAdminStatus()
    {
        return await _repo.GetAdminStatus();
    }

    public async Task<OrderViewModel> GetOrderDetails(string orderId)
    {
        return await _repo.GetOrderDetails(orderId);
    }

}
