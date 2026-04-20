using ClosedXML.Excel;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Cafe_Inventory_Management.Domain.Model;
using Cafe_Inventory_Management.Domain.IRepository;
using Cafe_Inventory_Management.Domain;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Cafe_Inventory_Management.Service
{
    public class EmailReportService
    {
        private readonly IConfiguration _config;
        private readonly IOrderRepo _repo;
        public EmailReportService(IConfiguration config, IOrderRepo repo)
        {
            _config = config;
            _repo = repo;
        }

        public async Task<string> SendReportAsync(bool isMonthly, bool scheduledOnly = false)
        {
            if (isMonthly)
            {
                var today = DateTime.Now;
                var tomorrow = today.AddDays(1);

                // Only run if tomorrow is next month (meaning today is last day)
                if (tomorrow.Month == today.Month)
                    return null;
            }
            var settings = _config.GetSection("EmailSettings");
            var nowUtc = DateTime.UtcNow;
            var (startDate, endDate) = GetReportRangeUtc(nowUtc, isMonthly);
            startDate = EnsureUtc(startDate);
            endDate = EnsureUtc(endDate);
            try
            {
                if (scheduledOnly && isMonthly && !IsLastDayOfMonthInMyanmar(nowUtc))
                {
                    return "skipped-monthly-not-last-day";
                }

                var orders = await _repo.GetOrdersByDate(startDate, endDate);
                var totalRevenue = orders.Sum(x => x.TotalPrice);

                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Sales Report");

                var colorHeaderBg = XLColor.FromHtml("#203A5F");
                var colorAccentBg = XLColor.FromHtml("#F3F6FA");
                var colorGrid = XLColor.FromHtml("#D9DEE7");
                var colorTextMuted = XLColor.FromHtml("#5B6470");

                ws.Style.Font.FontName = "Calibri";
                ws.Style.Font.FontSize = 11;

                ws.Range("A1:G1").Merge().Value = "CAFE INVENTORY MANAGEMENT";
                ws.Range("A2:G2").Merge().Value = isMonthly ? "MONTHLY SALES REPORT" : "DAILY SALES REPORT";
                ws.Range("A1:G2").Style.Font.Bold = true;
                ws.Range("A1:G1").Style.Font.FontSize = 14;
                ws.Range("A2:G2").Style.Font.FontSize = 12;
                ws.Range("A1:G2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Range("A1:G2").Style.Fill.BackgroundColor = XLColor.White;

                ws.Cell("A4").Value = "Report Period";
                ws.Cell("B4").Value = $"{startDate:dd MMM yyyy HH:mm} - {endDate:dd MMM yyyy HH:mm}";
                ws.Cell("A5").Value = "Generated At";
                ws.Cell("B5").Value = $"{DateTime.UtcNow:dd MMM yyyy HH:mm} UTC";
                ws.Cell("A6").Value = "Total Orders";
                ws.Cell("B6").Value = orders.Count;
                ws.Cell("A7").Value = "Total Revenue";
                ws.Cell("B7").Value = totalRevenue;
                ws.Cell("B7").Style.NumberFormat.Format = "#,##0 \"MMK\"";
                ws.Range("A4:A7").Style.Font.Bold = true;
                ws.Range("A4:G7").Style.Fill.BackgroundColor = colorAccentBg;
                ws.Range("A4:G7").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Range("A4:G7").Style.Border.OutsideBorderColor = colorGrid;

                int headerRow = 9;
                string[] headers = { "Order ID", "Staff Name", "Date Time", "Product / Item", "Qty", "Unit Price", "Subtotal" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(headerRow, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Fill.BackgroundColor = colorHeaderBg;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }

                int currentRow = headerRow + 1;
                foreach (var order in orders)
                {
                    var orderRange = ws.Range(currentRow, 1, currentRow, 7);
                    orderRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#EDF2F8");
                    orderRange.Style.Font.Bold = true;

                    ws.Cell(currentRow, 1).Value = order.OrderId;
                    ws.Cell(currentRow, 2).Value = order.CreatedBy;
                    ws.Cell(currentRow, 3).Value = order.CreatedAt.ToString("dd MMM yyyy HH:mm");
                    ws.Cell(currentRow, 4).Value = "Order Total";
                    ws.Cell(currentRow, 7).Value = order.TotalPrice;
                    ws.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0 \"MMK\"";
                    ws.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    currentRow++;

                    foreach (var item in order.Items)
                    {
                        ws.Cell(currentRow, 4).Value = item.ProductName;
                        ws.Cell(currentRow, 4).Style.Font.FontColor = colorTextMuted;
                        ws.Cell(currentRow, 5).Value = item.Quantity;
                        ws.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(currentRow, 6).Value = item.Amount;
                        ws.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0 \"MMK\"";
                        ws.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        ws.Cell(currentRow, 7).Value = item.Quantity * item.Amount;
                        ws.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0 \"MMK\"";
                        ws.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        currentRow++;
                    }
                }

                if (currentRow > headerRow + 1)
                {
                    var dataRange = ws.Range(headerRow, 1, currentRow - 1, 7);
                    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    dataRange.Style.Border.OutsideBorderColor = colorGrid;
                    dataRange.Style.Border.InsideBorderColor = colorGrid;
                }

                ws.Columns().AdjustToContents();
                ws.Column(4).Width = Math.Max(ws.Column(4).Width, 34);
                ws.SheetView.FreezeRows(headerRow);
                await ExecuteEmailSend(workbook, isMonthly, settings, startDate, endDate, orders.Count);
                return "true";
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static DateTime EnsureUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc) return value;
            if (value.Kind == DateTimeKind.Local) return value.ToUniversalTime();
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        private static bool IsLastDayOfMonthInMyanmar(DateTime utcNow)
        {
            var tz = GetMyanmarTimeZone();

            var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);
            return localNow.Day == DateTime.DaysInMonth(localNow.Year, localNow.Month);
        }

        private static (DateTime startUtc, DateTime endUtc) GetReportRangeUtc(DateTime utcNow, bool isMonthly)
        {
            var tz = GetMyanmarTimeZone();
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);

            if (isMonthly)
            {
                var localMonthStart = new DateTime(localNow.Year, localNow.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
                var monthStartUtc = TimeZoneInfo.ConvertTimeToUtc(localMonthStart, tz);
                return (monthStartUtc, utcNow);
            }

            var localDayStart = localNow.Date;
            var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDayStart, DateTimeKind.Unspecified), tz);
            return (dayStartUtc, utcNow);
        }

        private static TimeZoneInfo GetMyanmarTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Myanmar Standard Time");
            }
            catch
            {
                return TimeZoneInfo.Local;
            }
        }

        private async Task ExecuteEmailSend(XLWorkbook workbook, bool isMonthly, IConfigurationSection settings, DateTime start, DateTime end, int count)
        {
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileBytes = stream.ToArray();
            var adminEmails = await GetAdminEmails();
            if (adminEmails == null || !adminEmails.Any())
                return;
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(settings["SenderName"], settings["SenderEmail"]!));

            foreach (var email in adminEmails)
            {
                message.To.Add(MailboxAddress.Parse(email));
            }
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

        public async Task<string?> GetManagementToken()
        {
            var payload = new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", _config["Auth0:ClientId"]! },
                { "client_secret", _config["Auth0:ClientSecret"]! },
                { "audience", $"https://{_config["Auth0:Domain"]}/api/v2/" }
            };

            var url = $"https://{_config["Auth0:Domain"]}/oauth/token";

            try
            {
                var content = new FormUrlEncodedContent(payload);
                using var client = new HttpClient();

                var response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<Auth0TokenResponse>(jsonString);
                    return result?.access_token;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Auth0 Token Error: {error}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception getting Auth0 token: {ex.Message}");
                return null;
            }
        }

        public async Task<List<string>> GetAdminEmails()
        {
            var adminEmails = new List<string>();
            var token = await GetManagementToken();

            if (string.IsNullOrEmpty(token))
                return adminEmails;

            using var client = new HttpClient();

            var roleId = _config["Auth0:AdminRoleId"];

            var url = $"https://{_config["Auth0:Domain"]}/api/v2/roles/{roleId}/users";

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return adminEmails;

            var json = await response.Content.ReadAsStringAsync();

            var users = JsonConvert.DeserializeObject<List<Auth0User>>(json);

            if (users != null)
            {
                adminEmails = users
                    .Where(u => !string.IsNullOrEmpty(u.email))
                    .Select(u => u.email)
                    .ToList();
            }
            return adminEmails;
        }
    }
}
