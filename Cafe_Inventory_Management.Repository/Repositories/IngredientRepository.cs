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

        // 3. Pagination
        var items = await query
            .OrderBy(p => p.Name) // Sorting is required for Skip/Take
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Ingredients>
        {
            Items = items,
            TotalCount = totalCount
        };
    }

    public async Task<int> CreateIngredients(Ingredients ingredients)
    {
        await _context.Ingredients.AddAsync(ingredients);
        var result = _context.SaveChanges();
        return result;
    }

    public async Task<int> UpdateIngredients(Ingredients updateingredients)
    {
        var product = await _context.Ingredients.Where(x => x.Id == updateingredients.Id).FirstOrDefaultAsync();
        product.Name=updateingredients.Name;
        product.Amount=updateingredients.Amount;
        product.Quatity = updateingredients.Quatity;
        product.IsActive = updateingredients.IsActive;
        _context.Update(product);
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

}
