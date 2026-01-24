using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Cafe_Inventory_Management.UI.Services;
using Cafe_Inventory_Management.Domain;
using System.Net.Http.Headers;
using System.Text;
namespace Cafe_Inventory_Management.UI.Components.Pages;

public partial class Login : ComponentBase
{
    [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; }
    [Inject] public IApiCallService _apiService { get; set; }

    [Inject] public AuthServices _authService { get; set; }

    [Inject] public IConfiguration Configuration { get; set; }

    private string Username;
    private string Name;

    private string Password;
    private string Error;
    private bool IsLoading;
    private bool IsSignUp = false;

    private void ToggleMode()
    {
        IsSignUp = !IsSignUp;
        Error = null;
    }

    private async Task HandleSubmit()
    {
         await HandleLogin();
    }

    private async Task HandleLogin()
    {
        if (!ValidateInputs()) return;

        try
        {
            IsLoading = true;
            Error = null;

            var payload = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "username", Username },
                { "password", Password },
                { "client_id", Configuration["Auth0:ClientId"] },
                { "client_secret", Configuration["Auth0:ClientSecret"] },
                { "scope", "openid profile email" }
            };

            var url = $"https://{Configuration["Auth0:Domain"]}/oauth/token";
            var apirequest = new ApiRequest(HttpMethod.Post, url, payload,"aa");
            var response = await _apiService.APICall(apirequest);

            if (response != null && response.ErrorCode == "00")
            {
                var result = JsonConvert.DeserializeObject<Auth0TokenResponse>(response.Detail!);
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(result.id_token);
                var claims = jwt.Claims.ToList();
                var roleClaim = claims.FirstOrDefault(c => c.Type == "https://coffeeapp.com/roles");

                if (roleClaim != null)
                {
                    var roles = roleClaim.Value
                        .Replace("[", "")
                        .Replace("]", "")
                        .Replace("\"", "")
                        .Split(',', StringSplitOptions.RemoveEmptyEntries);

                    foreach (var role in roles)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role.Trim()));
                    }
                }

                if (AuthenticationStateProvider is CustomAuthStateProvider customAuthStateProvider)
                {
                    var name = claims.FirstOrDefault(c => c.Type == "name")?.Value ?? Username;
                    customAuthStateProvider.MarkUserAsAuthenticated(name, claims);
                    Navigation.NavigateTo($"/product");
                }
            }
            else
            {
                Error = "Login failed. Please check your email and password.";
            }
        }
        catch (Exception)
        {
            Error = "An error occurred while connecting to the server.";
        }
        finally { IsLoading = false; }
    }


    private bool ValidateInputs()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            Error = "Both email and password are required.";
            return false;
        }
        return true;
    }

  

   
    //private async Task HandleRegister()
    //{
    //    IsLoading = true;
    //    Error = null;

    //    var success = await CreateUser(Username, Password, Name);

    //    if (success)
    //    {
    //        Navigation.NavigateTo("/");
    //    }
    //    else
    //    {
    //        Error = "Registration failed. Try again.";
    //    }

    //    IsLoading = false;
    //}

}

