using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cafe_Inventory_Management.Domain;

public class Auth0User
{
    public string user_id { get; set; }
    public string email { get; set; }
    public string name { get; set; }
    public bool email_verified { get; set; }
    public DateTime created_at { get; set; }
}

