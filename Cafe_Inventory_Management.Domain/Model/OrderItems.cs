using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cafe_Inventory_Management.Domain.Model;
public class OrderItems:BaseEntity
{
    public string OrderId { get; set; }
    public string ProductCode { get; set; }
    public int Quatity { get; set; }
    public  decimal Amount { get; set; }
}


