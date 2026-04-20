using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cafe_Inventory_Management.Domain.Model; // Need this for OrderItems if it's there

namespace Cafe_Inventory_Management.Domain.Model;

public class OrderViewModel
{
    public string OrderId { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }

    public string? CreatedBy { get; set; }
    public List<OrderItems> Items { get; set; } = new();
}
