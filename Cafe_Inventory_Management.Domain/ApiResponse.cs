using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cafe_Inventory_Management.Domain
{
    public class ApiResponse
    {
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string? Detail { get; set; }

    }
}
