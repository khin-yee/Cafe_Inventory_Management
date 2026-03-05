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

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Business Sales Report");

            var colorHeaderBg = XLColor.FromHtml("#1A73E8"); // Corporate Blue
            var colorOrderRowBg = XLColor.FromHtml("#E8F0FE"); // Very Light Blue
            var colorTextSecondary = XLColor.FromHtml("#5F6368"); // Professional Gray

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
                    ws.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    currentRow++;
                }

                ws.Range(currentRow - 1, 1, currentRow - 1, 7).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                ws.Range(currentRow - 1, 1, currentRow - 1, 7).Style.Border.BottomBorderColor = XLColor.FromHtml("#E0E0E0");
            }

            ws.Columns().AdjustToContents();
            ws.Column(4).Width = 40;
            await ExecuteEmailSend(workbook, isMonthly, settings, startDate, endDate, orders.Count);
            return "true";
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
            message.From.Add(new MailboxAddress(settings["SenderName"], settings["SenderEmail"]));

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
                { "client_id", _config["Auth0:ClientId"] },
                { "client_secret", _config["Auth0:ClientSecret"] },
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

            var roleId = _config["Auth0:AdminRoleId"]; // store in appsettings

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


        //public async Task<List<Auth0User>> GetUsers()
        //{
        //    var token = await GetManagementToken();

        //    var url = $"https://{_config["Auth0:Domain"]}/api/v2/users";

        //    using var client = new HttpClient();

        //    client.DefaultRequestHeaders.Authorization =
        //        new AuthenticationHeaderValue("Bearer", token);

        //    var response = await client.GetAsync(url);

        //    var json = await response.Content.ReadAsStringAsync();

        //    return JsonConvert.DeserializeObject<List<Auth0User>>(json);
        //}

        //public async Task<List<Auth0User>> GetAllUsersWithRoles()
        //{
        //    // 1. Get the list of all users
        //    var users = await GetUsers(); // Your existing method
        //    var token = await GetManagementToken();

        //    if (string.IsNullOrEmpty(token)) return users;

        //    using var client = new HttpClient();


        //    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        //    // 2. For each user, fetch their roles
        //    // Note: In a large system, you'd use app_metadata to avoid multiple calls
        //    foreach (var user in users)
        //    {
        //        var roleUrl = $"https://{_config["Auth0:Domain"]}/api/v2/users/{user.user_id}/roles";
        //        var roleResponse = await client.GetAsync(roleUrl);

        //        if (roleResponse.IsSuccessStatusCode)
        //        {
        //            var roleJson = await roleResponse.Content.ReadAsStringAsync();
        //            var userroles = JsonConvert.DeserializeObject<List<Auth0Role>>(roleJson) ?? new();
        //            user.roles = 
        //        }
        //    }

        //    return users;
        //}
    }
}
