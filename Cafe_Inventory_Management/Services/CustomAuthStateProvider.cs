using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Cafe_Inventory_Management.UI.Services;
public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private ClaimsPrincipal _user = new ClaimsPrincipal(new ClaimsIdentity());

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(_user));
    }

    public void MarkUserAsAuthenticated(string userName, IEnumerable<Claim> claims)
    {
        var identity = new ClaimsIdentity(claims, "apiauth_type");
        _user = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void MarkUserAsLoggedOut()
    {
        _user = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void UpdateUserName(string newName)
    {
        if (_user.Identity is ClaimsIdentity identity)
        {
            var existingClaim = identity.FindFirst("name");
            if (existingClaim != null)
            {
                identity.RemoveClaim(existingClaim);
            }
            identity.AddClaim(new Claim("name", newName));
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}