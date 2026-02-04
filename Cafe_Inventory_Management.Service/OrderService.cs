using Cafe_Inventory_Management.Domain.IRepository;
using Cafe_Inventory_Management.Domain.IServices;
using Cafe_Inventory_Management.Domain.Model;
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

    public async Task<string> CreateOrder(OrderRequestDto order)
    {
        var result = await _repo.SaveOrder(order);
        if (result != 1)
            return "fail";

        else
            return "success";
    }
}
