using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cafe_Inventory_Management.Domain.Model;
public class Orders:BaseEntity
{
    public  string OrderId { get; set; }
    public  decimal TotalPrice { get; set; }
    public string Status { get; set; }

}

