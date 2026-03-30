using Microsoft.AspNetCore.Mvc;
using RPGEconomy.Application.Abstractions.Services;

namespace RPGEconomy.API.Controllers;

[Route("api/products")]
[ApiController]
public class ProductTypesController : ControllerBase
{
    private readonly IProductTypeService _productTypeService;

    public ProductTypesController(IProductTypeService productTypeService)
        => _productTypeService = productTypeService;

    // GET: api/products
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _productTypeService.GetAllAsync();
        return Ok(result.Value);
    }

    // GET api/products/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _productTypeService.GetByIdAsync(id);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    // POST api/products
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductTypeRequest request)
    {
        var result = await _productTypeService.CreateAsync(
            request.Name, request.Description, request.BasePrice, request.WeightPerUnit);

        if (!result.IsSuccess) return BadRequest(result.Error);
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    // PUT api/products/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateProductTypeRequest request)
    {
        var result = await _productTypeService.UpdateAsync(
            id, request.Name, request.Description, request.BasePrice, request.WeightPerUnit);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    // DELETE api/products/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _productTypeService.DeleteAsync(id);
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }
}

public record CreateProductTypeRequest(
    string Name,
    string Description,
    decimal BasePrice,
    double WeightPerUnit);
