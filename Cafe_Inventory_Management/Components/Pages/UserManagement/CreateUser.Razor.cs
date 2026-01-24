using Cafe_Inventory_Management.UI.Services;
using Microsoft.AspNetCore.Components;

namespace Cafe_Inventory_Management.UI.Components.Pages.UserManagement;
public partial class CreateUser:ComponentBase
{
    [Inject] public AuthServices _authService { get; set; }

    string Email, Name, Password;

    async Task Save()
    {

    }

}

