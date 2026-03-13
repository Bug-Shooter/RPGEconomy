using Microsoft.AspNetCore.Mvc;
using RPGEconomy.Application.Abstractions.Services;

namespace RPGEconomy.API.Controllers;

[Route("api/worlds")]
[ApiController]
public class WorldsController : ControllerBase
{
    private readonly IWorldService _worldService;

    public WorldsController(IWorldService worldService)
        => _worldService = worldService;

    // GET: api/worlds
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _worldService.GetAllAsync();
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    // GET api/worlds/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _worldService.GetByIdAsync(id);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    // POST api/worlds
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorldRequest request)
    {
        var result = await _worldService.CreateAsync(request.Name, request.Description);
        if (!result.IsSuccess) return BadRequest(result.Error);

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    // PUT api/worlds/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateWorldRequest request)
    {
        var result = await _worldService.UpdateAsync(id, request.Name, request.Description);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    // DELETE api/worlds/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _worldService.DeleteAsync(id);
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }
}

public record CreateWorldRequest(string Name, string Description);
