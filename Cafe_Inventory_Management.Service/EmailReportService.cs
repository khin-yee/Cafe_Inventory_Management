using ClosedXML.Excel;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Cafe_Inventory_Management.Domain.Model;
using Cafe_Inventory_Management.Domain.IRepository;

namespace Cafe_Inventory_Management.Service
{
    public class EmailReportService
    {
        private readonly IConfiguration _config;
        private readonly IOrderRepo _repo; // Your DB Context

        public EmailReportService(IConfiguration config, IOrderRepo repo)
        {
            _config = config;
            _repo = repo;
        }

        public async Task<string> SendReportAsync(bool isMonthly)
        {
            var settings = _config.GetSection("EmailSettings");

            // 1. Date Range Setup
            var startDate = isMonthly
                ? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1)
                : DateTime.Now.Date.AddDays(-1);

            var endDate = isMonthly
                ? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59)
                : DateTime.Now.Date.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);

            var orders = await _repo.GetOrders();

            // 3. Create Excel Workbook
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Business Sales Report");

            // --- Define Professional Color Palette ---
            var colorHeaderBg = XLColor.FromHtml("#1A73E8"); // Corporate Blue
            var colorOrderRowBg = XLColor.FromHtml("#E8F0FE"); // Very Light Blue
            var colorTextSecondary = XLColor.FromHtml("#5F6368"); // Professional Gray

            // --- Set Headers ---
            string[] headers = { "Order ID", "Staff Name", "Date Time", "Product / Item", "Qty", "Unit Price", "Subtotal" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = colorHeaderBg;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int currentRow = 2;

            foreach (var order in orders)
            {
                // --- MAIN ORDER DATA ROW (The "Anchor" Row) ---
                var orderRange = ws.Range(currentRow, 1, currentRow, 7);
                orderRange.Style.Fill.BackgroundColor = colorOrderRowBg;
                orderRange.Style.Font.Bold = true;
                orderRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                orderRange.Style.Border.TopBorderColor = XLColor.FromHtml("#ADCCFB");

                ws.Cell(currentRow, 1).Value = order.OrderId;
                ws.Cell(currentRow, 2).Value = order.CreatedBy;
                ws.Cell(currentRow, 3).Value = order.CreatedAt.ToString("g");
                ws.Cell(currentRow, 4).Value = "ORDER TOTAL";
                ws.Cell(currentRow, 7).Value = order.TotalPrice;
                ws.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0 \"MMK\"";

                currentRow++;

                // --- PRODUCT ITEM ROWS ---
                foreach (var item in order.Items)
                {
                    ws.Cell(currentRow, 4).Value = "   • " + item.ProductName;
                    ws.Cell(currentRow, 4).Style.Font.FontColor = colorTextSecondary;

                    ws.Cell(currentRow, 5).Value = item.Quatity;
                    ws.Cell(currentRow, 6).Value = item.Amount;
                    ws.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0";

                    ws.Cell(currentRow, 7).Value = item.Quatity * item.Amount;
                    ws.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0";
                    ws.Cell(currentRow, 7).Style.Font.FontColor = colorTextSecondary;

                    // Alignments
                    ws.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    currentRow++;
                }

                // Add a small spacer border after the items are done
                ws.Range(currentRow - 1, 1, currentRow - 1, 7).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                ws.Range(currentRow - 1, 1, currentRow - 1, 7).Style.Border.BottomBorderColor = XLColor.FromHtml("#E0E0E0");
            }

            ws.Columns().AdjustToContents();
            ws.Column(4).Width = 40; // Give the product name extra width

            // 4. Send the Email
            await ExecuteEmailSend(workbook, isMonthly, settings, startDate, endDate, orders.Count);
            return "true";
        }

        private async Task ExecuteEmailSend(XLWorkbook workbook, bool isMonthly, IConfigurationSection settings, DateTime start, DateTime end, int count)
        {
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileBytes = stream.ToArray();

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(settings["SenderName"], settings["SenderEmail"]));
            message.To.Add(new MailboxAddress("Admin", settings["AdminEmail"]));
            message.Subject = $"{(isMonthly ? "MONTHLY" : "DAILY")} Sales Report: {start:dd MMM}";

            var body = new BodyBuilder { HtmlBody = $"<p>Attached is the report for {start:dd MMM yyyy}. Total Orders: <b>{count}</b></p>" };
            body.Attachments.Add(isMonthly ? "Monthly_Report.xlsx" : "Daily_Report.xlsx", fileBytes);
            message.Body = body.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(settings["Host"], int.Parse(settings["Port"]), SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(settings["SenderEmail"], settings["Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
