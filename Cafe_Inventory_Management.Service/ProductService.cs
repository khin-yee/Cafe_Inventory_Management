using Cafe_Inventory_Management.Domain;
using Cafe_Inventory_Management.Domain.IRepository;
using Cafe_Inventory_Management.Domain.IServices;
using Cafe_Inventory_Management.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cafe_Inventory_Management.Service;
public class ProductService:IProductService
{
    private readonly IProductRepo _repo;

    public ProductService(IProductRepo repo)
    {
        _repo = repo;
    }

    public async Task<List<Product>> GetAllProducts()
    {
        return await _repo.GetProducts();
    }

    public async Task<PagedResult<Product>> GetPagedProducts(int pageNumber, int pageSize, string? searchTerm)
    {
        return  await _repo.GetPagedProducts(pageNumber, pageSize, searchTerm);
    }

    public async Task<ApiResponse> CreateProduct(Product product, List<ProductIngredients> ingredients)
  
    {
        return await _repo.CreateProduct(product, ingredients);
        
    }

    public async Task<ApiResponse> UpdateProduct(Product product, List<ProductIngredients> ingredients)
    {
         return await _repo.UpdateProduct(product,ingredients); 
    }

    public async Task<string> DeleteProduct(int id)
    {
        var result = await _repo.DeleteProduct(id);
        if (result != 1)
            return "fail";

        else
            return "success";
    }

    public async Task<string> CreateProductList(List<Product> products)
    {
        var result = await _repo.CreateProductList(products);
        if (result != 1)
            return "fail";
        else
            return "success";

    }

}

