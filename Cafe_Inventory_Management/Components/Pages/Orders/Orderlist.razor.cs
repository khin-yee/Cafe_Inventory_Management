using Cafe_Inventory_Management.Domain.Model;
using Cafe_Inventory_Management.Domain;
using Cafe_Inventory_Management.UI.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components.Authorization;

namespace Cafe_Inventory_Management.UI.Components.Pages.Orders
{
    public partial class Orderlist:ComponentBase
    {
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IDialogService DialogService { get; set; }

        [Inject] AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

        private List<Product> _products = new();
        private List<CartItem> _cart = new();
        private string _searchString = "";
        private bool _loading = true;
        [Inject] public IApiCallService _apiService { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await LoadProducts();
        }

        private async Task LoadProducts()
        {
            _loading = true;
            try
            {
                var url = $"/Product";
                var request = new ApiRequest(HttpMethod.Get, url, "", "");

                var response = await _apiService.APICall(request);

                if (response != null && response.ErrorCode == "00")
                {
                    var data = JsonConvert.DeserializeObject<PagedResult<Product>>(response.Detail);
                    _products = data.Items;

                }
            }
            finally
            {
                _loading =false;
            }
            //try
            //{
            //    // Ensure your API returns all items or use pagination params
            //    // Note: Filter for IsActive is handled in the UI Chip display
            //    var response = a;
            //    _products = response ?? new();
            //}
            //finally
            //{
            //    _loading = false;
            //}
        }

        private void AddToCart(Product product)
        {
            var existing = _cart.FirstOrDefault(x => x.ProductId == product.Id);
            if (existing != null)
            {
                existing.Quantity++;
            }
            else
            {
                _cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductCode = product.Code,
                    ProductName = product.Name,
                    Price = product.Amount,
                    Quantity = 1
                });
            }
            Snackbar.Add($"{product.Name} added to cart", Severity.Info);
        }

        private void RemoveFromCart(CartItem item)
        {
            _cart.Remove(item);
        }

        private async Task PlaceOrder()
        {
            // Simple delete-style confirmation logic without the error-prone 'YesActionColor'
            bool? result = await DialogService.ShowMessageBox(
                "Confirm Order",
                "Are you sure you want to process this transaction?",
                yesText: "Confirm", cancelText: "Cancel");

            if (result == true)
            {
                var authState = await AuthStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;
                var url =  $"/CreateOrder" ;
                var requestModel = new OrderRequestDto()
                {
                    Items = _cart,
                    UserName = user.FindFirst(c => c.Type == "name")?.Value?? "Unknown User"
                };
                var request = new ApiRequest(HttpMethod.Post, url, requestModel, "");
                var response = await _apiService.APICall(request);


                if (response.ErrorCode == "00")
                {
                    var apires = JsonConvert.DeserializeObject<ApiResponse>(response.Detail);
                    if (apires.ErrorCode == "00")
                    {
                        Snackbar.Add("Order Placed Successfully!", Severity.Success);
                        _cart.Clear();
                        await LoadProducts();
                    }
                    else
                    {
                        Snackbar.Add(apires.ErrorMessage, Severity.Error);

                    }// Reload to reflect any inventory changes
                }
                else
                {
                    Snackbar.Add("Error placing order. Check inventory levels.", Severity.Error);
                }
            }
        }

        
    }
}
