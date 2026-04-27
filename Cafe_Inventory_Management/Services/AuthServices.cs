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

        var users = JsonConvert.DeserializeObject<List<Auth0User>>(json)
                    .OrderByDescending(x => x.created_at)
                    .ToList();

        var roles = await GetRoles(token);

        var roleTasks = roles.Select(async role =>
        {
            var roleUsers = await GetUsersByRole(role.id, token);

            foreach (var roleUser in roleUsers)
            {
                var user = users.FirstOrDefault(u => u.user_id == roleUser.user_id);

                if (user != null)
                {
                    user.roles ??= new List<string>();
                    user.roles.Add(role.name);
                }
            }
        });

        await Task.WhenAll(roleTasks);

        return users;
    }
    public async Task DeleteUser(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            throw new Exception("UserId is required");

        var token = await GetManagementToken();
        if (string.IsNullOrEmpty(token))
            throw new Exception("Failed to get Management token");

        var encodedUserId = Uri.EscapeDataString(userId);
        var url = $"https://{_configuration["Auth0:Domain"]}/api/v2/users/{encodedUserId}";

        var request = new ApiRequest(HttpMethod.Delete, url, null, token);

        var response = await _apiService.APICall(request);

        if (response == null || response.ErrorCode != "00")
            throw new Exception(response?.ErrorMessage ?? response?.Detail ?? "DeleteUser failed");
    }
    public async Task<List<Auth0User>> GetUsersByRole(string roleId, string token)
    {
        var url = $"https://{_configuration["Auth0:Domain"]}/api/v2/roles/{roleId}/users";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(url);
        var json = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<List<Auth0User>>(json);
    }
    public async Task<List<Auth0Role>> GetRoles(string token)
    {
        var url = $"https://{_configuration["Auth0:Domain"]}/api/v2/roles";
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(url);
        var json = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<List<Auth0Role>>(json);
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

        var encodedUserId = Uri.EscapeDataString(userId);
        var url = $"https://{_configuration["Auth0:Domain"]}/api/v2/users/{encodedUserId}/roles";

        var request = new ApiRequest(HttpMethod.Post, url, body, token);
        var response = await _apiService.APICall(request);

        if (response == null || response.ErrorCode != "00")
            throw new Exception($"AssignRole failed: {response?.Detail}");
    }

    public async Task UpdateUser(string id, string email, string name)
    {
        var token = await GetManagementToken();

        var encodedUserId = Uri.EscapeDataString(id);
        var url = $"https://{_configuration["Auth0:Domain"]}/api/v2/users/{encodedUserId}";

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

        var encodedUserId = Uri.EscapeDataString(id);
        var url = $"https://{_configuration["Auth0:Domain"]}/api/v2/users/{encodedUserId}";

        var client = new HttpClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var json = await client.GetStringAsync(url);

        return JsonConvert.DeserializeObject<Auth0User>(json);
    }

    public async Task UpdateProfile(string id, string name, string? password)
    {
        var token = await GetManagementToken();

        var encodedUserId = Uri.EscapeDataString(id);
        var url = $"https://{_configuration["Auth0:Domain"]}/api/v2/users/{encodedUserId}";

        var bodyObj = new Dictionary<string, object>();
        bodyObj["name"] = name;
        if (!string.IsNullOrWhiteSpace(password))
        {
            bodyObj["password"] = password;
            bodyObj["connection"] = "Username-Password-Authentication";
        }

        var json = JsonConvert.SerializeObject(bodyObj);

        var client = new HttpClient();

        var req = new HttpRequestMessage(HttpMethod.Patch, url);

        req.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        req.Content =
            new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.SendAsync(req);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            var errorMessage = "An unknown error occurred while updating the profile.";
            try
            {
                var parsedError = JsonConvert.DeserializeObject<dynamic>(errorBody);
                if (parsedError?.message != null)
                {
                    errorMessage = parsedError.message;
                    // Clean up specific known prefixes from Auth0 like "PasswordStrengthError:"
                    if (errorMessage.StartsWith("PasswordStrengthError:"))
                    {
                        errorMessage = errorMessage.Replace("PasswordStrengthError:", "").Trim();
                    }
                }
            }
            catch
            {
                // Fallback if parsing fails
                errorMessage = $"Status {response.StatusCode}: {errorBody}";
            }

            throw new Exception(errorMessage);
        }
    }
}
