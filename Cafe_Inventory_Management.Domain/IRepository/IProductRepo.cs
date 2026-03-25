using Cafe_Inventory_Management.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cafe_Inventory_Management.Domain.IRepository;
public interface IProductRepo
{
    Task<List<Product>> GetProducts();
    Task<ApiResponse> CreateProduct(Product product, List<ProductIngredients> ingredients);
    Task<PagedResult<Product>> GetPagedProducts(int pageNumber, int pageSize, string? searchTerm);
    Task<ApiResponse> UpdateProduct(Product product, List<ProductIngredients> ingredients);
    Task<int> DeleteProduct(int id);
    Task<int> CreateProductList(List<Product> products);

}

