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
    private readonly EmailReportService _emailService;

    public OrderController(IOrderService service, EmailReportService emailService)
    {
        _service = service;
        _emailService = emailService;
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
        List<OrderViewModel> data = await _service.ExcelExport(search, startDate, endDate);

        using (var xl = new XLWorkbook())
        {
            var ws = xl.Worksheets.Add("Order History");

            var colorHeaderBg = XLColor.FromHtml("#1A73E8"); // Corporate Blue
            var colorOrderRowBg = XLColor.FromHtml("#E8F0FE"); // Very Light Blue
            var colorTextSecondary = XLColor.FromHtml("#5F6368"); // Professional Gray

            var headerStyle = xl.Style;
            headerStyle.Font.Bold = true;
            headerStyle.Fill.BackgroundColor = colorTextSecondary; 
            headerStyle.Font.FontColor = XLColor.Black;

            var orderRowStyle = xl.Style;
            orderRowStyle.Fill.BackgroundColor = XLColor.FromHtml("#E8F0FE"); // Light Blue Highlight
            orderRowStyle.Font.Bold = true;

            string[] headers = { "Date", "Order ID", "Staff Name", "Product / Code", "Qty", "Unit Price", "Total (MMK)" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style = headerStyle;
            }

            int currentRow = 2;

            foreach (var order in data)
            {
                var orderRange = ws.Range(currentRow, 1, currentRow, 7);
                orderRange.Style = orderRowStyle;

                ws.Cell(currentRow, 1).Value = order.CreatedAt.ToString("g");
                ws.Cell(currentRow, 2).Value = order.OrderId;
                ws.Cell(currentRow, 3).Value = order.CreatedBy; // Added Staff Name
                ws.Cell(currentRow, 4).Value = "ORDER SUMMARY";
                ws.Cell(currentRow, 7).Value = order.TotalPrice;
                ws.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0 \"MMK\"";

                currentRow++;

                foreach (var item in order.Items)
                {
                    ws.Cell(currentRow, 4).Value = $"   • {item.ProductName} ({item.ProductCode})";
                    ws.Cell(currentRow, 4).Style.Font.FontColor = XLColor.DimGray;

                    ws.Cell(currentRow, 5).Value = item.Quatity;
                    ws.Cell(currentRow, 6).Value = item.Amount;
                    ws.Cell(currentRow, 7).Value = (item.Quatity * item.Amount);

                    ws.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0";
                    ws.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0";
                    ws.Cell(currentRow, 7).Style.Font.FontColor = XLColor.Gray;

                    currentRow++;
                }

                ws.Range(currentRow - 1, 1, currentRow - 1, 7).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                ws.Range(currentRow - 1, 1, currentRow - 1, 7).Style.Border.BottomBorderColor = XLColor.LightGray;
            }

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

    [HttpPost("/EmailToAdmin")]
    public async Task<IActionResult> EmailToAdmin([FromBody] bool montly)
    {
        return Ok(await _emailService.SendReportAsync(montly));
    }
}
