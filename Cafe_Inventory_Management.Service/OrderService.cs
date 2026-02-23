using Cafe_Inventory_Management.Domain;
using Cafe_Inventory_Management.Domain.IRepository;
using Cafe_Inventory_Management.Domain.IServices;
using Cafe_Inventory_Management.Domain.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

}
