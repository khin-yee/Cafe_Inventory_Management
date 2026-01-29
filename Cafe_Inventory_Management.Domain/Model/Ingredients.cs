using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cafe_Inventory_Management.Domain.Model;
public class Ingredients:BaseEntity
{
    public string Name { get; set; }
    public string Code { get; set; }
    public int Quatity { get; set; }
    public string Unit { get; set; }
    public decimal Amount { get; set; }
}


