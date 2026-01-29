using Cafe_Inventory_Management.Domain;
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

    public async Task<PagedResult<Product>> GetPagedProducts(int pageNumber, int pageSize, string? searchTerm)
    {
        var query = _context.Product.AsQueryable();

        // 1. Filtering
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(p => p.Name.Contains(searchTerm));
        }

        // 2. Get Total Count (before pagination)
        var totalCount = await query.CountAsync();

        // 3. Pagination
        var items = await query
            .OrderBy(p => p.Name) // Sorting is required for Skip/Take
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Product>
        {
            Items = items,
            TotalCount = totalCount
        };
    }

    public async Task<int> CreateProduct(Product product)
    {
        await _context.Product.AddAsync(product);
        var result =_context.SaveChanges();
        return result;
    }

    public async Task<int>UpdatePrduct(Product updateProduct)
    {
        var product = await _context.Product.Where(x => x.Id == updateProduct.Id).FirstOrDefaultAsync();
        product.Name=updateProduct.Name;
        product.Amount=updateProduct.Amount;
        product.Quatity = updateProduct.Quatity;
        product.IsActive = updateProduct.IsActive;
        _context.Update(product);
        var result = _context.SaveChanges();
        return result;
    }

    public async Task<int> DeleteProduct(int id)
    {
        var product = await _context.Product.Where(x => x.Id == id).FirstOrDefaultAsync();
        _context.Remove(product);
        var result = _context.SaveChanges();
        return result;
    }

}

