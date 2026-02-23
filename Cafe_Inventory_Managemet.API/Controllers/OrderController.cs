using Cafe_Inventory_Management.Domain.IServices;
using Cafe_Inventory_Management.Domain.Model;
using Cafe_Inventory_Management.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Cafe_Inventory_Managemet.API.Controllers;
[Route("api/[controller]")]
[ApiController]
public class OrderController : ControllerBase
{
    private readonly IOrderService _service;

    public OrderController(IOrderService service)
    {
        _service = service;
    }

    [HttpPost("/CreateOrder")]
    public async Task<IActionResult> CreateOrder([FromBody] OrderRequestDto order)
    {
        return Ok(await _service.CreateOrder(order));
    }

    [HttpPut("/UpdateOrder")]
    public async Task<IActionResult> UpdateOrder([FromBody] OrderViewModel order)
    {
        return Ok(await _service.UpdateOrder(order));
    }

    [HttpPut("/UpdateOrderStatus")]
    public async Task<IActionResult> UpdateOrderStatus([FromBody] OrderViewModel order)
    {
        return Ok(await _service.UpdateOrderStatus(order));
    }

    [HttpGet("/GetOrders")]
    public async Task<IActionResult> GetOrders()
    {
        return Ok(await _service.GetOrders());
    }

    [HttpGet("/GetOrderSummary")]
    public async Task<IActionResult> GetOrdersSummary([FromQuery] int page, [FromQuery] int pageSize, [FromQuery] string? search, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        return Ok(await _service.GetAllSuccessOrders(page,pageSize,search,startDate,endDate));
    }


}

