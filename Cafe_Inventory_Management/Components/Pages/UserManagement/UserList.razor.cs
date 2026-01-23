using Cafe_Inventory_Management.Domain;
using Cafe_Inventory_Management.UI.Services;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace Cafe_Inventory_Management.UI.Components.Pages.UserManagement;
public partial class UserList : ComponentBase
{

    [Inject] public AuthServices _authService { get; set; }

    List<Auth0User> Users = new();
    string SearchText = "";

    protected override async Task OnInitializedAsync()
    {
        Users = await _authService.GetUsers();
    }

    List<Auth0User> FilteredUsers =>
        Users.Where(u =>
            string.IsNullOrEmpty(SearchText) ||
            u.email.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
            u.name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
        ).ToList();

    void CreateUser()
    {
        Nav.NavigateTo("/users/create");
    }

    void EditUser(string id)
    {
        Nav.NavigateTo($"/users/edit/{id}");
    }
}




