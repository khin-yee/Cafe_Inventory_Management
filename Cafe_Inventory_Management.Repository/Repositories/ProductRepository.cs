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
            .OrderByDescending(p => p.Id) // Newest first
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
            if (product == null || string.IsNullOrWhiteSpace(product.Code))
            {
                await transaction.RollbackAsync();
                return new ApiResponse { ErrorCode = "99", ErrorMessage = "Product code is required." };
            }

            var normalizedCode = product.Code.Trim();
            product.Code = normalizedCode;
            var incomingQty = product.Quatity ?? 0;

            var existingProduct = await _context.Product.FirstOrDefaultAsync(x => x.Code == normalizedCode);
            var targetProduct = product;

            if (existingProduct == null)
            {
                product.CreatedAt = GetDbTimestamp();
                product.UpdatedAt = GetDbTimestamp();
                await _context.Product.AddAsync(product);
            }
            else
            {
                existingProduct.Name = product.Name;
                existingProduct.Category = product.Category;
                existingProduct.Amount = product.Amount;
                existingProduct.IsActive = product.IsActive;
                existingProduct.IsRecipe = product.IsRecipe;
                existingProduct.Quatity = (existingProduct.Quatity ?? 0) + incomingQty;
                existingProduct.UpdatedAt = GetDbTimestamp();
                existingProduct.UpdatedBy = product.CreatedBy ?? product.UpdatedBy;
                targetProduct = existingProduct;
            }

            var validRecipe = (ingredients ?? new List<ProductIngredients>())
                .Where(x => !string.IsNullOrWhiteSpace(x.IngredientCode) && x.RequiredAmount > 0)
                .ToList();

            foreach (var item in validRecipe)
            {
                item.ProductCode = normalizedCode;
                var ingredientCode = item.IngredientCode.Trim();
                item.IngredientCode = ingredientCode;

                var existingMap = await _context.ProductIngredients
                    .FirstOrDefaultAsync(x => x.ProductCode == normalizedCode && x.IngredientCode == ingredientCode);

                if (existingMap == null)
                {
                    item.CreatedAt = GetDbTimestamp();
                    item.UpdatedAt = GetDbTimestamp();
                    await _context.ProductIngredients.AddAsync(item);
                }
                else
                {
                    existingMap.RequiredAmount = item.RequiredAmount;
                    existingMap.IsActive = item.IsActive;
                    existingMap.UpdatedAt = GetDbTimestamp();
                    existingMap.UpdatedBy = item.CreatedBy ?? item.UpdatedBy;
                }
            }

            if (!targetProduct.IsRecipe && incomingQty > 0 && validRecipe.Any())
            {
                foreach (var item in validRecipe)
                {
                    var stock = await _context.Ingredients.FirstOrDefaultAsync(x => x.Code == item.IngredientCode);
                    if (stock == null)
                    {
                        continue;
                    }

                    decimal totalNeeded = incomingQty * item.RequiredAmount;
                    stock.Quatity -= totalNeeded;
                    _context.Ingredients.Update(stock);
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var message = existingProduct == null
                ? "Product created successfully."
                : "Product already exists. Quantity increased successfully.";

            return new ApiResponse { ErrorCode = "00", ErrorMessage = message };
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

    public async Task<ApiResponse> CreateProductList(List<ProductRequest> products)
    {
        if (products == null || !products.Any())
        {
            return new ApiResponse { ErrorCode = "99", ErrorMessage = "No products to import." };
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var request in products)
            {
                if (request?.Product == null || string.IsNullOrWhiteSpace(request.Product.Code))
                {
                    await transaction.RollbackAsync();
                    return new ApiResponse { ErrorCode = "99", ErrorMessage = "Product code is required for import." };
                }

                var product = request.Product;
                var normalizedCode = product.Code.Trim();
                product.Code = normalizedCode;

                var incomingQty = product.Quatity ?? 0;
                var existingProduct = await _context.Product.FirstOrDefaultAsync(x => x.Code == normalizedCode);
                var targetProduct = product;

                if (existingProduct == null)
                {
                    product.CreatedAt = GetDbTimestamp();
                    product.UpdatedAt = GetDbTimestamp();
                    await _context.Product.AddAsync(product);
                }
                else
                {
                    existingProduct.Name = product.Name;
                    existingProduct.Category = product.Category;
                    existingProduct.Amount = product.Amount;
                    existingProduct.IsActive = product.IsActive;
                    existingProduct.IsRecipe = product.IsRecipe;
                    existingProduct.Quatity = (existingProduct.Quatity ?? 0) + incomingQty;
                    existingProduct.UpdatedAt = GetDbTimestamp();
                    existingProduct.UpdatedBy = product.CreatedBy ?? product.UpdatedBy;
                    targetProduct = existingProduct;
                }

                var incomingRecipe = (request.Recipe ?? new List<ProductIngredients>())
                    .Where(x => !string.IsNullOrWhiteSpace(x.IngredientCode) && x.RequiredAmount > 0)
                    .ToList();

                var deductionRecipe = new List<ProductIngredients>();
                if (incomingRecipe.Any())
                {
                    foreach (var item in incomingRecipe)
                    {
                        item.ProductCode = normalizedCode;
                        var ingredientCode = item.IngredientCode.Trim();
                        item.IngredientCode = ingredientCode;

                        var existingMap = await _context.ProductIngredients
                            .FirstOrDefaultAsync(x => x.ProductCode == normalizedCode && x.IngredientCode == ingredientCode);

                        if (existingMap == null)
                        {
                            item.CreatedAt = GetDbTimestamp();
                            item.UpdatedAt = GetDbTimestamp();
                            await _context.ProductIngredients.AddAsync(item);
                            deductionRecipe.Add(item);
                        }
                        else
                        {
                            existingMap.RequiredAmount = item.RequiredAmount;
                            existingMap.IsActive = item.IsActive;
                            existingMap.UpdatedAt = GetDbTimestamp();
                            existingMap.UpdatedBy = item.CreatedBy ?? item.UpdatedBy;
                            deductionRecipe.Add(existingMap);
                        }
                    }
                }
                else
                {
                    deductionRecipe = await _context.ProductIngredients
                        .Where(x => x.ProductCode == normalizedCode)
                        .ToListAsync();
                }

                if (!deductionRecipe.Any())
                {
                    await transaction.RollbackAsync();
                    return new ApiResponse
                    {
                        ErrorCode = "99",
                        ErrorMessage = $"Product '{normalizedCode}' must include at least one ingredient."
                    };
                }

                if (!targetProduct.IsRecipe && incomingQty > 0)
                {
                    foreach (var item in deductionRecipe)
                    {
                        var stock = await _context.Ingredients.FirstOrDefaultAsync(x => x.Code == item.IngredientCode);
                        if (stock != null)
                        {
                            decimal totalNeeded = incomingQty * item.RequiredAmount;
                            stock.Quatity -= totalNeeded;
                            _context.Ingredients.Update(stock);
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new ApiResponse { ErrorCode = "00", ErrorMessage = "Import successful." };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return new ApiResponse { ErrorCode = "99", ErrorMessage = ex.Message };
        }
    }

    private static DateTime GetDbTimestamp()
    {
        return DateTime.UtcNow;
    }

}

