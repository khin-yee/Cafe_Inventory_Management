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

    public async Task<ApiResponse> CreateProduct(Product product)
    {
        var response = new ApiResponse();
        using var transaction = await _context.Database.BeginTransactionAsync();
        if (product.IsRecipe == false)
        {
            var res = await _context.ProductIngredients.Where(x => x.ProductCode==product.Code).ToListAsync();
            foreach(var item in res)
            {
                var ingredient = await _context.Ingredients.Where(x=>x.Code==item.IngredientCode).FirstOrDefaultAsync();

                if (ingredient == null)
                {
                    response.ErrorCode = "01";
                    response.ErrorMessage = $"Ingredient with ID {item.IngredientCode} not found.";
                    return response;
                }
                decimal totalDeduction = (product.Quatity ?? 0) * (item.RequiredAmount );

                if (ingredient.Quatity < totalDeduction)
                {
                    response.ErrorCode = "01";
                    response.ErrorMessage = $"Stock too low for {ingredient.Name}. Needed: {totalDeduction}";
                    return response;
                }

                ingredient.Quatity -= totalDeduction;
                _context.Ingredients.Update(ingredient);
            }
        }
        await _context.Product.AddAsync(product);
        _context.SaveChanges();
        await transaction.CommitAsync();
        return response;
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

