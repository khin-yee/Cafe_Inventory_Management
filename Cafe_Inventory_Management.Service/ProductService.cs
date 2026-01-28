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


}

