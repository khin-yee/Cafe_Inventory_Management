using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cafe_Inventory_Management.Domain.Model
{
    public class OrderRequestDto
    {
        public List<CartItem> Items { get; set; } = new();
        public string UserName { get; set; }
        public string? Note { get; set; }
    }

    public class CartItem
    {
        public string ProductCode { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }

    }
}
