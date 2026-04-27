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
    readonly HashSet<string> _deletedUserIds = new();

    string Search = "";
    bool IsLoading = true;

    protected override async Task OnInitializedAsync()
    {
        IsLoading = true;
        Users = await LoadUsers();
        IsLoading = false;
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
            try
            {
                await _authService.DeleteUser(userId);
                _deletedUserIds.Add(userId);
                Users = Users.Where(user => user.user_id != userId).ToList();
                StateHasChanged();

                Snackbar.Add("User deleted successfully", Severity.Success);

                _ = RefreshAfterDelete();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Failed to delete user: {ex.Message}", Severity.Error);
            }
        }
    }

    async Task RefreshAfterDelete()
    {
        await Task.Delay(1500);
        await Reload(showSuccessMessage: false);
    }

    async Task Reload()
        => await Reload(showSuccessMessage: true);

    async Task Reload(bool showSuccessMessage)
    {
        IsLoading = true;
        StateHasChanged();
        
        Users = await LoadUsers();
        
        IsLoading = false;
        StateHasChanged();

        if (showSuccessMessage)
        {
            Snackbar.Add("User list updated", Severity.Success);
        }
    }

    async Task<List<Auth0User>> LoadUsers()
    {
        var users = await _authService.GetUsers();
        return users.Where(user => !_deletedUserIds.Contains(user.user_id)).ToList();
    }
}




