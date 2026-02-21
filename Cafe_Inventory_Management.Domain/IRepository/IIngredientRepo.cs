using Cafe_Inventory_Management.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cafe_Inventory_Management.Domain.IRepository;
public interface IIngredientRepo
{
    Task<List<Ingredients>> GetIngredients();
    Task<int> CreateIngredients(Ingredients ingredients);
    Task<PagedResult<Ingredients>> GetPagedIngredients(int pageNumber, int pageSize, string? searchTerm);
    Task<int> UpdateIngredients(Ingredients updateIngredients);
    Task<int> DeleteIngredients(int id);
    Task<int> CreateIngredientsList(List<Ingredients> ingredients);
    Task<List<Ingredients>> GetLowStockIngredientsAsync();



}
