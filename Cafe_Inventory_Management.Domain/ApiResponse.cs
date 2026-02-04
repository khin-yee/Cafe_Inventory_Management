using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cafe_Inventory_Management.Domain
{
    public class ApiResponse
    {
        public string ErrorCode { get; set; } = "00";
        public string ErrorMessage { get; set; } = "No Error";
        public string? Detail { get; set; }

    }
}
