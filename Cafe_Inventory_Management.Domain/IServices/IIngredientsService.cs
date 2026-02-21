using Cafe_Inventory_Management.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cafe_Inventory_Management.Domain.IServices;
public interface IIngredientsService
{
    Task<List<Ingredients>> GetAllIngredients();
    Task<PagedResult<Ingredients>> GetPagedIngredients(int pageNumber, int pageSize, string? searchTerm);
    Task<string> CreateIngredients(Ingredients ingredients);
    Task<string> UpdateIngredients(Ingredients ingredients);
    Task<string> DeleteIngredients(int id);
    Task<string> CreateIngredientsList(List<Ingredients> ingredients);
    Task<List<Ingredients>> GetLowStockIngredientsAsync();

}

