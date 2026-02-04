using Cafe_Inventory_Management.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cafe_Inventory_Management.Domain.IServices;
public interface IProductService
{
    Task<List<Product>> GetAllProducts();
    Task<PagedResult<Product>> GetPagedProducts(int pageNumber, int pageSize, string? searchTerm);
    Task<ApiResponse> CreateProduct(Product product);
    Task<string> UpdateProduct(Product product);
    Task<string> DeleteProduct(int id);

}

