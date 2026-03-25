using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cafe_Inventory_Management.Domain.Model
{
    public class ProductRequest
    {
        public Product Product { get; set; }
        public List<ProductIngredients> Recipe { get; set; }
    }
}
