using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cafe_Inventory_Management.Domain.Model;

public class DashboardData
{
    public decimal TotalSales { get; set; }
    public int TotalOrders { get; set; }
    public decimal TodaySales { get; set; }
    public decimal AvgOrderValue { get; set; }

    // --- Chart Data (Bar/Line) ---
    public string[] ChartLabels { get; set; } = Array.Empty<string>();
    public double[] ChartData { get; set; } = Array.Empty<double>();

    // --- Category Data (Pie/Donut) ---
    public List<CategoryStat> CategoryDistribution { get; set; } = new();

    // --- Ranking Data ---
    public List<ProductStat> TopProducts { get; set; } = new();
}

public class ProductStat
{
    public string ProductName { get; set; }
    public double TotalQty { get; set; }
}

public class CategoryStat
{
    public string CategoryName { get; set; } = string.Empty;
    public double TotalRevenue { get; set; }
}


