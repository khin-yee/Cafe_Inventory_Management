using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cafe_Inventory_Management.Domain.Model;
public class OrdersModel:BaseEntity
{
    public  string OrderId { get; set; }
    public  decimal TotalPrice { get; set; }
    public string Status { get; set; }

}

public class OrderViewModel
{
    public string OrderId { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItems> Items { get; set; } = new();
}