using Cafe_Inventory_Management.Repository;
using Cafe_Inventory_Management.Service;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("connectionstring")));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddRepo().AddService();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("connectionstring")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString))); // 

builder.Services.AddHangfireServer();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// In Program.cs after app.MapRazorComponents<App>()
using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    TimeZoneInfo reportTimeZone;
    try
    {
        reportTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Myanmar Standard Time");
    }
    catch
    {
        reportTimeZone = TimeZoneInfo.Local;
    }

    recurringJobManager.AddOrUpdate<EmailReportService>(
        "daily-admin-report",
        service => service.SendReportAsync(false, true),
        "0 22 * * *",
        new RecurringJobOptions { TimeZone = reportTimeZone });

    recurringJobManager.AddOrUpdate<EmailReportService>(
     "monthly-admin-report",
     service => service.SendReportAsync(true, true),
     "0 23 28-31 * *",
     new RecurringJobOptions { TimeZone = reportTimeZone });
}

app.UseHangfireDashboard(); 

app.MapControllers();


app.Run();
