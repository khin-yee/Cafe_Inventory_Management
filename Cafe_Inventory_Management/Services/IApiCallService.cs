using Cafe_Inventory_Management.Domain;

namespace Cafe_Inventory_Management.UI.Services
{
    public interface IApiCallService
    {
        Task<ApiResponse> APICall(ApiRequest apiRequest);
    }
}
