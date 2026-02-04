using Cafe_Inventory_Management.Domain.IServices;
using Cafe_Inventory_Management.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Cafe_Inventory_Managemet.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IngredientsController : ControllerBase
    {
        private readonly IIngredientsService _IngredientsService;

        public IngredientsController(IIngredientsService ingredientsService)
        {
            _IngredientsService = ingredientsService;
        }

        [HttpGet("/Ingredients")]
        public async Task<IActionResult> GetAllIngredients(
         [FromQuery] int pageNumber = 1,
         [FromQuery] int pageSize = 5,
         [FromQuery] string? search = null)
        {
            // Pass parameters to the service layer
            var result = await _IngredientsService.GetPagedIngredients(pageNumber, pageSize, search);
            return Ok(result);
        }

        [HttpPost("/CreateIngredients")]
        public async Task<IActionResult> CreateIngredients([FromBody] Ingredients Ingredients)
        {
            return Ok(await _IngredientsService.CreateIngredients(Ingredients));
        }

        [HttpPut("/UpdateIngredients")]
        public async Task<IActionResult> UpdateIngredients([FromBody] Ingredients Ingredients)
        {
            return Ok(await _IngredientsService.UpdateIngredients(Ingredients));
        }

        [HttpDelete("/DeleteIngredients")]
        public async Task<IActionResult> DeleteIngredients([FromBody] int id)
        {
            return Ok(await _IngredientsService.DeleteIngredients(id));
        }

        [HttpPost("BulkUpload")]
        public async Task<IActionResult> BulkUpload([FromBody] List<Ingredients> ingredients)
        {           
            return Ok(await _IngredientsService.CreateIngredientsList(ingredients));
        }
    }
}
