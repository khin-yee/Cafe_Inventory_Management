using Cafe_Inventory_Management.Domain.IRepository;
using Cafe_Inventory_Management.Domain.IServices;
using Cafe_Inventory_Management.Domain.Model;
using Cafe_Inventory_Management.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cafe_Inventory_Management.Service;
public class IngredientsService : IIngredientsService
{
    private readonly IIngredientRepo _repo;

    public IngredientsService(IIngredientRepo repo)
    {
        _repo = repo;
    }

    public async Task<List<Ingredients>> GetAllIngredients()
    {
        return await _repo.GetIngredients();
    }

    public async Task<PagedResult<Ingredients>> GetPagedIngredients(int pageNumber, int pageSize, string? searchTerm)
    {
        return await _repo.GetPagedIngredients(pageNumber, pageSize, searchTerm);
    }

    public async Task<string> CreateIngredients(Ingredients ingredients)
    {
        var result = await _repo.CreateIngredients(ingredients);
        if (result != 1)
            return "fail";

        else
            return "success";
    }

    public async Task<string> UpdateIngredients(Ingredients ingredients)
    {
        var result = await _repo.UpdateIngredients(ingredients);
        if (result != 1)
            return "fail";

        else
            return "success";
    }

    public async Task<string> DeleteIngredients(int id)
    {
        var result = await _repo.DeleteIngredients(id);
        if (result != 1)
            return "fail";

        else
            return "success";
    }
}

