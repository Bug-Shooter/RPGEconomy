using Microsoft.AspNetCore.Mvc;
using RPGEconomy.Application.Abstractions.Services;

namespace RPGEconomy.API.Controllers;

[Route("api/resources")]
[ApiController]
public class ResourceTypesController : ControllerBase
{
    private readonly IResourceTypeService _resourceTypeService;

    public ResourceTypesController(IResourceTypeService resourceTypeService)
        => _resourceTypeService = resourceTypeService;

    // GET: api/resources
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search)
    {
        var result = string.IsNullOrWhiteSpace(search)
            ? await _resourceTypeService.GetAllAsync()
            : await _resourceTypeService.SearchByNameAsync(search);
        return Ok(result.Value);
    }

    // GET api/resources/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _resourceTypeService.GetByIdAsync(id);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    // POST api/resources
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateResourceTypeRequest request)
    {
        var result = await _resourceTypeService.CreateAsync(
            request.Name, request.Description,
            request.IsRenewable, request.RegenerationRatePerDay);

        if (!result.IsSuccess) return BadRequest(result.Error);
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    // PUT api/resources/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateResourceTypeRequest request)
    {
        var result = await _resourceTypeService.UpdateAsync(
            id, request.Name, request.Description,
            request.IsRenewable, request.RegenerationRatePerDay);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    // DELETE api/resources/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _resourceTypeService.DeleteAsync(id);
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }
}

public record CreateResourceTypeRequest(
    string Name,
    string Description,
    bool IsRenewable,
    double RegenerationRatePerDay);
