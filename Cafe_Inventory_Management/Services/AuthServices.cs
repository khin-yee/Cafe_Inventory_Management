using Cafe_Inventory_Management.Domain;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using static System.Net.WebRequestMethods;

namespace Cafe_Inventory_Management.UI.Services;
public class AuthServices
{
    public readonly AuthenticationStateProvider _authenticationStateProvider;
    public readonly IApiCallService _apiService;
    public readonly IConfiguration _configuration;

    public AuthServices(AuthenticationStateProvider authenticationStateProvider, IApiCallService apiService, IConfiguration configuration)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _apiService = apiService;
        _configuration = configuration;
    }

    private readonly Dictionary<string, string> RoleMap = new()
{
    { "Admin", "rol_jsfDrWBoLrbjf6PP" },
    { "Staff", "rol_NwqwJ1SurxmFeyjn" },
};

    public async Task<string?> GetManagementToken()
    {
        var payload = new Dictionary<string, string>
                        {
                            { "grant_type", "client_credentials" },
                            { "client_id", _configuration["Auth0:ClientId"] },
                            { "client_secret", _configuration["Auth0:ClientSecret"] },
                            { "audience", $"https://{_configuration["Auth0:Domain"]}/api/v2/" }
                        };

        var url = $"https://{_configuration["Auth0:Domain"]}/oauth/token";

        var request = new ApiRequest(HttpMethod.Post, url, payload, "aa");
        var response = await _apiService.APICall(request);

        if (response != null && response.ErrorCode == "00")
        {
            var result = JsonConvert.DeserializeObject<Auth0TokenResponse>(response.Detail!);
            return result?.access_token;
        }

        return null;
    }
    public async Task<List<Auth0User>> GetUsers()
    {
        var token = await GetManagementToken();

        var url = $"https://{_configuration["Auth0:Domain"]}/api/v2/users";

        using var client = new HttpClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(url);

        var json = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<List<Auth0User>>(json);
    }
    public async Task<string> CreateUser(string email, string password, string userName)
    {
        var token = await GetManagementToken();
        if (string.IsNullOrEmpty(token))
            throw new Exception("Failed to get Management token");

        var url = $"https://{_configuration["Auth0:Domain"]}/api/v2/users";

        var body = new
        {
            email = email,
            password = password,
            username = userName,
            name= userName,
            connection = "Username-Password-Authentication"
        };

        // Use ApiCallService instead of new HttpClient
        var request = new ApiRequest(HttpMethod.Post, url, body, token);
        var response = await _apiService.APICall(request);

        if (response == null || response.ErrorCode != "00")
            throw new Exception($"CreateUser failed: {response?.Detail}");

        var user = JsonConvert.DeserializeObject<Auth0User>(response.Detail!);

        if (user == null || string.IsNullOrEmpty(user.user_id))
            throw new Exception("Failed to parse Auth0 user");

        return user.user_id;
    }

    public async Task AssignRole(string userId, string roleName)
    {
        if (string.IsNullOrEmpty(userId))
            throw new Exception("UserId is null or empty");

        var token = await GetManagementToken();
        if (string.IsNullOrEmpty(token))
            throw new Exception("Failed to get Management Token");

        var roleId = RoleMap[roleName];

        var body = new { roles = new[] { roleId } };

        var url = $"https://{_configuration["Auth0:Domain"]}/api/v2/users/{userId}/roles";

        var request = new ApiRequest(HttpMethod.Post, url, body, token);
        var response = await _apiService.APICall(request);

        if (response == null || response.ErrorCode != "00")
            throw new Exception($"AssignRole failed: {response?.Detail}");
    }

    public async Task UpdateUser(string id, string email, string name)
    {
        var token = await GetManagementToken();

        var url = $"https://{_configuration["Auth0:Domain"]}/api/v2/users/{id}";

        var body = new
        {
            email = email,
            name = name
        };

        var json = JsonConvert.SerializeObject(body);

        var client = new HttpClient();

        var req = new HttpRequestMessage(HttpMethod.Patch, url);

        req.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        req.Content =
            new StringContent(json, Encoding.UTF8, "application/json");

        await client.SendAsync(req);
    }
    public async Task<Auth0User> GetUser(string id)
    {
        var token = await GetManagementToken();

        var url = $"https://{_configuration["Auth0:Domain"]}/api/v2/users/{id}";

        var client = new HttpClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var json = await client.GetStringAsync(url);

        return JsonConvert.DeserializeObject<Auth0User>(json);
    }

   



}
