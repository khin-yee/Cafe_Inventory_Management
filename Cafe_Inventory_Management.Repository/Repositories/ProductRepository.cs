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

    public async Task<ApiResponse> CreateProduct(Product product, List<ProductIngredients> ingredients)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Save the Product
            await _context.Product.AddAsync(product);

            // 2. Save the Recipe Mapping
            await _context.ProductIngredients.AddRangeAsync(ingredients);

            // 3. CHECK LOGIC: If NOT a Recipe (Batch Production), deduct ingredients NOW
            if (!product.IsRecipe)
            {
                foreach (var item in ingredients)
                {
                    var stock = await _context.Ingredients.FirstOrDefaultAsync(x => x.Code == item.IngredientCode);
                    if (stock != null)
                    {
                        decimal totalNeeded = (product.Quatity ?? 0) * item.RequiredAmount;
                        stock.Quatity -= totalNeeded;
                        _context.Ingredients.Update(stock);
                    }
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return new ApiResponse { ErrorCode = "00" };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return new ApiResponse { ErrorCode = "99", ErrorMessage = ex.Message };
        }
    }

    public async Task<ApiResponse> UpdateProduct(Product product, List<ProductIngredients> ingredients)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Update the main Product details
            var existingProduct = await _context.Product
                .FirstOrDefaultAsync(x => x.Code == product.Code);

            if (existingProduct == null)
                return new ApiResponse { ErrorCode = "99", ErrorMessage = "Product not found." };

            // Update fields
            existingProduct.Name = product.Name;
            existingProduct.Category = product.Category;
            existingProduct.Amount = product.Amount;
            existingProduct.IsActive = product.IsActive;
            existingProduct.IsRecipe = product.IsRecipe;
            existingProduct.Quatity = product.Quatity;
            _context.Product.Update(existingProduct);
            await _context.SaveChangesAsync();

            var oldRecipe = await _context.ProductIngredients
                .Where(x => x.ProductCode == product.Code)
                .ToListAsync();
            _context.ProductIngredients.RemoveRange(oldRecipe);

            if (ingredients != null && ingredients.Any())
            {
                foreach (var item in ingredients)
                {
                    item.ProductCode = product.Code;
                    await _context.ProductIngredients.AddAsync(item);
                }
            }

 

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return new ApiResponse { ErrorCode = "00" };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return new ApiResponse { ErrorCode = "99", ErrorMessage = ex.Message };
        }
    }

    public async Task<int> DeleteProduct(int id)
    {
        var product = await _context.Product.Where(x => x.Id == id).FirstOrDefaultAsync();
        _context.Remove(product);
        var result = _context.SaveChanges();
        return result;
    }

    public async Task<int> CreateProductList(List<Product> products)
    {
        await _context.Product.AddRangeAsync(products);
        var result = await _context.SaveChangesAsync();
        return result;
    }

}

