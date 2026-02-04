using Cafe_Inventory_Management.Domain;
using Cafe_Inventory_Management.Domain.IServices;
using Cafe_Inventory_Management.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Cafe_Inventory_Managemet.API.Controllers;
[Route("api/[controller]")]
[ApiController]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet("/Product")]
    public async Task<IActionResult> GetAllProduct(
     [FromQuery] int pageNumber = 1,
     [FromQuery] int pageSize = 5,
     [FromQuery] string? search = null)
    {
        // Pass parameters to the service layer
        var result = await _productService.GetPagedProducts(pageNumber, pageSize, search);
        return Ok(result);
    }

    [HttpPost("/CreateProduct")]
    public async Task<ApiResponse> CreateProduct([FromBody] Product product)
    {
        return await _productService.CreateProduct(product);
    }

    [HttpPut("/UpdateProduct")]
    public async Task<IActionResult>UpdateProduct([FromBody]Product product)
    {
        return Ok(await _productService.UpdateProduct(product));
    }

    [HttpDelete("/DeleteProduct")]
    public async Task<IActionResult> DeleteProduct([FromBody] int id)
    {
        return Ok(await _productService.DeleteProduct(id));
    }
}

