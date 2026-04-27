using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cafe_Inventory_Management.Domain;

public class Auth0User
{
    public string user_id { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public bool email_verified { get; set; }
    public DateTime created_at { get; set; }

    public List<string> roles { get; set; } = new();
}


public class Auth0Role
{
    public string id { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
}

