using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cafe_Inventory_Management.Domain.Model;
public class ProductIngredients:BaseEntity
{
    public string ProductCode { get; set; }
    public string IngredientCode { get; set; }
    public decimal RequiredAmount { get; set; }

}

