using Cafe_Inventory_Management.Domain;
using Cafe_Inventory_Management.Domain.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Cafe_Inventory_Managemet.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IOrderService _service;

        public AdminController(IOrderService service)
        {
            _service = service;
        }

        [HttpGet("/GetStatus")]
        public async Task<IActionResult> GetStats([FromQuery] string range)
        {
            return Ok(await _service.GetStats(range));
        }

        [HttpGet("/GetStaffPerformance")]
        public async Task<IActionResult> GetStaffPerformance([FromQuery] string range = "7d")
        {
           return Ok(await _service.GetStaffPerformance(range));
        }

        [HttpGet("/GetAdminStatus")]
        public async Task<IActionResult> GetAdminStatus()
        {
            return Ok(await _service.GetAdminStatus());
        }

    }
}
