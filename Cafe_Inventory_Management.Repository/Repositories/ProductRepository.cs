using Cafe_Inventory_Management.Domain.IRepository;
using Cafe_Inventory_Management.Domain.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Cafe_Inventory_Management.Repository.Repositories;
public class ProductRepository:IProductRepo
{
    protected readonly ApplicationDbContext _context;
    public ProductRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Product>> GetProducts()
    {
        try
        {
            var res = await _context.Product.OrderByDescending(p => p.Id).ToListAsync();
            return res;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<List<Product>> GetProductByName(string name)
    {
        try
        {
            var res = await _context.Product.Where(x=>x.Name==name ).ToListAsync();
            return res;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

  
}

