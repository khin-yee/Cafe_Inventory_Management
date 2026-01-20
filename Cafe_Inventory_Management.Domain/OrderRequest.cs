using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cafe_Inventory_Management.Domain
{
    public class OrderRequest
    {
        public string CustomerName { get; set; }

        // List of items in the order
        public List<OrderItemRequest> Items { get; set; } = new();

        // Optional: Table number or Takeaway status
        public string OrderType { get; set; } = "Dine-In";

        public string ProcessedByStaffId { get; set; }
    }

    public class OrderItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string SpecialInstructions { get; set; }
    }
}
