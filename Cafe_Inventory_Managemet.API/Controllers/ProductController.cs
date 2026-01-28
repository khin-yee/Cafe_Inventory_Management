using Cafe_Inventory_Management.Domain.IServices;
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

    public async Task<IActionResult> GetAllProduct()
    {
        return Ok(await _productService.GetAllProducts());
    }
}

