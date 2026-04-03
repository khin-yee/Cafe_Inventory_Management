using Cafe_Inventory_Management.Domain.IRepository;
using Cafe_Inventory_Management.Domain.Model;
using Cafe_Inventory_Management.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cafe_Inventory_Management.Repository.Repositories;

public class IngredientRepository : IIngredientRepo
{
    protected readonly ApplicationDbContext _context;
    public IngredientRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Ingredients>> GetIngredients()
    {
        try
        {
            var res = await _context.Ingredients.OrderByDescending(p => p.Id).ToListAsync();
            return res;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<List<Ingredients>> GetIngredientsByName(string name)
    {
        try
        {
            var res = await _context.Ingredients.Where(x => x.Name==name).ToListAsync();
            return res;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<PagedResult<Ingredients>> GetPagedIngredients(int pageNumber, int pageSize, string? searchTerm)
    {
        var query = _context.Ingredients.AsQueryable();

        // 1. Filtering
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(p => p.Name.Contains(searchTerm));
        }

        // 2. Get Total Count (before pagination)
        var totalCount = await query.CountAsync();

        try
        {
            // 3. Pagination
            var items = await query
                .OrderByDescending(p => p.Id) // Newest first
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return new PagedResult<Ingredients>
            {
                Items = items,
                TotalCount = totalCount
            };
        }
        catch(Exception ex)
        {
            throw ex;
        }
        
    }

    public async Task<int> CreateIngredients(Ingredients ingredients)
    {
        if (ingredients == null || string.IsNullOrWhiteSpace(ingredients.Code))
        {
            return 0;
        }

        await UpsertIngredient(ingredients);
        return await _context.SaveChangesAsync();
    }

    public async Task<int> UpdateIngredients(Ingredients updateingredients)
    {
        var product = await _context.Ingredients.Where(x => x.Id == updateingredients.Id).FirstOrDefaultAsync();
        product.Name=updateingredients.Name;
        product.Amount=updateingredients.Amount;
        product.Quatity = updateingredients.Quatity;
        product.IsActive = updateingredients.IsActive;
        var result = _context.SaveChanges();
        return result;
    }

    public async Task<int> DeleteIngredients(int id)
    {
        var product = await _context.Ingredients.Where(x => x.Id == id).FirstOrDefaultAsync();
        _context.Remove(product);
        var result = _context.SaveChanges();
        return result;
    }

    public async Task<int> CreateIngredientsList(List<Ingredients> ingredients)
    {
        if (ingredients == null || !ingredients.Any())
        {
            return 0;
        }

        foreach (var item in ingredients)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Code))
            {
                continue;
            }

            await UpsertIngredient(item);
        }

        return await _context.SaveChangesAsync();
    }

    private async Task UpsertIngredient(Ingredients incoming)
    {
        var code = incoming.Code.Trim();
        var existing = await _context.Ingredients.FirstOrDefaultAsync(x => x.Code == code);

        if (existing == null)
        {
            incoming.Code = code;
            incoming.CreatedAt = GetDbTimestamp();
            incoming.UpdatedAt = GetDbTimestamp();
            await _context.Ingredients.AddAsync(incoming);
            return;
        }

        existing.Name = incoming.Name;
        existing.Unit = incoming.Unit;
        existing.Amount = incoming.Amount;
        existing.MinStockLevel = incoming.MinStockLevel;
        existing.IsActive = incoming.IsActive;
        existing.UpdatedAt = GetDbTimestamp();
        existing.UpdatedBy = incoming.CreatedBy ?? incoming.UpdatedBy;
        existing.Quatity += incoming.Quatity;
    }

    public async Task<List<Ingredients>> GetLowStockIngredientsAsync()
    {
        return await _context.Ingredients
            .Where(i => i.Quatity <= i.MinStockLevel)
            .ToListAsync();
    }

    private static DateTime GetDbTimestamp()
    {
        return DateTime.UtcNow;
    }
}
