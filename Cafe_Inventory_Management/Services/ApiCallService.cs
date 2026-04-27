using Cafe_Inventory_Management.Domain;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using Newtonsoft.Json;


namespace Cafe_Inventory_Management.UI.Services;

public class ApiCallService : IApiCallService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public ApiCallService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }
    public async Task<ApiResponse> APICall(ApiRequest apiRequest)
    {
        var responseModel = new ApiResponse();
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(130);
            var request = PopulateHttpRequestMessage(apiRequest);
            if (!string.IsNullOrWhiteSpace(apiRequest.token))
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiRequest.token);
            }
            try
            {
                var response = await httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                responseModel.ErrorCode = "00";
                responseModel.ErrorMessage = "No Error";
                responseModel.Detail = await response.Content.ReadAsStringAsync();
            }
            else
            {
                responseModel.ErrorCode = "01";
                var errorContent = await response.Content.ReadAsStringAsync();
                responseModel.ErrorMessage = "System Error"; // Fallback
                
                try 
                {
                    // Try to extract a message if the API returns a structured error
                    var errorObj = JsonConvert.DeserializeObject<dynamic>(errorContent);
                    if (errorObj?.message != null)
                    {
                        responseModel.ErrorMessage = errorObj.message;
                    }
                }
                catch { /* Ignore parsing errors and keep fallback */ }

                responseModel.Detail = errorContent;
            }

            }
            catch (Exception)
            {
                throw;
            }
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            responseModel.ErrorCode = "04";
            responseModel.ErrorMessage = ex.Message;
            responseModel.Detail = string.Empty;
        }
        catch (Exception ex)
        {
            responseModel.ErrorCode = "99";
            responseModel.ErrorMessage = ex.Message;
            responseModel.Detail = string.Empty;
        }
        return responseModel;
    }

    private HttpRequestMessage PopulateHttpRequestMessage(ApiRequest apiRequest)
    {
        bool isFormContent = (apiRequest.requestBody?.GetType()== typeof(MultipartFormDataContent));
        string baseUrl;
        if (string.IsNullOrEmpty(apiRequest.token))
        {
            baseUrl = $"{_configuration["ApiURl:baseurl"]}{apiRequest.url}";
        }
        else
        {
            baseUrl = apiRequest.url;
        }
        HttpContent? content = null;
        if (isFormContent)
        {
            content = apiRequest.requestBody as MultipartFormDataContent;
        }
        else if (apiRequest.requestBody != null)
        {
            content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(apiRequest.requestBody), Encoding.UTF8, MediaTypeNames.Application.Json);
        }

        var httpRequestMsg = new HttpRequestMessage(apiRequest.method, baseUrl)
        {
            Content = content
        };

        httpRequestMsg.Headers.UserAgent.ParseAdd("HttpRequestsSample");
        httpRequestMsg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(isFormContent ? "*/*" : "application/json"));
        return httpRequestMsg;

    }
}
