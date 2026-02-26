using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cafe_Inventory_Management.Domain
{
    public class StaffResponseDto
    {
        public string StaffName { get; set; } 
        public int TotalOrdersHandled { get; set; }
        public decimal TotalRevenueGenerated { get; set; }
     }

    public class AdminDashboardData
    {
        public List<LowStockProduct> LowStockItems { get; set; } = new();
        public List<UserActivity> RecentActivities { get; set; } = new();
    }

    public class LowStockProduct
    {
        public string Name { get; set; }
        public double CurrentStock { get; set; }
        public string Category { get; set; }
    }

    public class UserActivity
    {
        public string User { get; set; }
        public string Action { get; set; }
        public DateTime Time { get; set; }
    }

}
