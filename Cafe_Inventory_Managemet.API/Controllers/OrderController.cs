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

}

