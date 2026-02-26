using Cafe_Inventory_Management.Domain;
using Cafe_Inventory_Management.Domain.IServices;
using Cafe_Inventory_Management.Domain.Model;
using Cafe_Inventory_Management.Service;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data;

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
        return Ok(await _service.GetAllSuccessOrders(page, pageSize, search, startDate, endDate));
    }

    [HttpGet("/ExportExcel")]
    public async Task<IActionResult> GetExcelExport([FromQuery] string? search, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var data = await _service.ExcelExport(search, startDate, endDate);

        DataTable dt = new DataTable("OrderHistory");
        dt.Columns.AddRange(new DataColumn[] {
                    new DataColumn("Date"),
                    new DataColumn("Order ID"),
                    new DataColumn("Product Code"),
                    new DataColumn("Qty"),
                    new DataColumn("Unit Price"),
                    new DataColumn("Item Total"),
                    new DataColumn("Order Grand Total")
                });

        foreach (var order in data)
        {
            bool firstItem = true;
            foreach (var item in order.Items)
            {
                dt.Rows.Add(
                    order.CreatedAt.ToString("g"),
                    order.OrderId,
                    item.ProductCode,
                    item.Quatity, 
                    item.Amount,
                    (item.Quatity * item.Amount),
                    firstItem ? order.TotalPrice : 0 
                );
                firstItem = false;
            }
        }

        using (var xl = new XLWorkbook())
        {
            var ws = xl.Worksheets.Add(dt);

            ws.Row(1).Style.Font.Bold = true;
            ws.Columns().AdjustToContents();

            using (MemoryStream mstream = new MemoryStream())
            {
                xl.SaveAs(mstream);
                return File(
                    mstream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Detailed_Report_{DateTime.Now:yyyyMMdd}.xlsx"
                );
            }
        }
    }


    [HttpGet("/GetOrderDetails/{orderId}")]
    public async Task<IActionResult> GetOrderDetails(string orderId)
    {          
        return Ok(await _service.GetOrderDetails(orderId));
    }
}
