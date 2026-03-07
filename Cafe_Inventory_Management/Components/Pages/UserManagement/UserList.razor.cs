using Cafe_Inventory_Management.Domain;
using Cafe_Inventory_Management.UI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace Cafe_Inventory_Management.UI.Components.Pages.UserManagement;
public partial class UserList : ComponentBase
{
    [Inject] public AuthServices _authService { get; set; }

    List<Auth0User> Users = new();

    string Search = "";

    protected override async Task OnInitializedAsync()
    {
        Users = await _authService.GetUsers();
    }

    IEnumerable<Auth0User> FilteredUsers => Users.Where(user =>
    {
        if (string.IsNullOrWhiteSpace(Search))
            return true;

        return (user.name?.Contains(Search, StringComparison.OrdinalIgnoreCase) ?? false) ||
               (user.email?.Contains(Search, StringComparison.OrdinalIgnoreCase) ?? false);
    });

    async Task OpenCreate()
    {
        var dialog = DialogService.Show<UserDialog>("Create ");

        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await Reload();
        }
    }

    async Task OpenEdit(Auth0User user)
    {
        var param = new DialogParameters
        {
            ["User"] = user,
            ["IsEdit"] = true
        };

        var dialog = DialogService.Show<UserDialog>("Edit User", param);

        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await Reload();
        }
    }
    private async Task ConfirmDeleteUser(string userId)
    {
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraSmall,
            FullWidth = true,
            CloseButton = true,

        };
        bool? result = await DialogService.ShowMessageBox(
         new MessageBoxOptions
         {
             Title = "Warning",
             MarkupMessage = new MarkupString("Are you sure you want to delete this user? <br/><b>This action cannot be undone!</b>"),
             YesText = "Delete",
             CancelText = "Cancel",
         },
         options
         );

        if (result == true)
        {
            await _authService.DeleteUser(userId);
            await Reload();
        }
    }
    async Task Reload()
    {
        Users = await _authService.GetUsers();
        StateHasChanged();

        Snackbar.Add("User list updated", Severity.Success);
    }
}




