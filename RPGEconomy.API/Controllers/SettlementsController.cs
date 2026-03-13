using Microsoft.AspNetCore.Mvc;
using RPGEconomy.Application.Abstractions.Services;

namespace RPGEconomy.API.Controllers;

[Route("api/worlds/{worldId}/settlements")]
[ApiController]
public class SettlementsController : ControllerBase
{
    private readonly ISettlementService _settlementService;

    public SettlementsController(ISettlementService settlementService)
        => _settlementService = settlementService;

    // GET: api/worlds/{worldId}/settlements
    [HttpGet]
    public async Task<IActionResult> GetByWorld(int worldId)
    {
        var result = await _settlementService.GetByWorldIdAsync(worldId);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    // GET api/worlds/{worldId}/settlements/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int worldId, int id)
    {
        var result = await _settlementService.GetByIdAsync(id);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    // POST api/worlds/{worldId}/settlements
    [HttpPost]
    public async Task<IActionResult> Create(
    int worldId,
    [FromBody] CreateSettlementRequest request)
    {
        var result = await _settlementService.CreateAsync(worldId, request.Name, request.Population);
        if (!result.IsSuccess) return BadRequest(result.Error);

        return CreatedAtAction(nameof(GetById),
            new { worldId, id = result.Value!.SettlementId },
            result.Value);
    }

    // PUT api/worlds/{worldId}/settlements/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        int worldId, int id,
        [FromBody] CreateSettlementRequest request)
    {
        var result = await _settlementService.UpdateAsync(id, request.Name, request.Population);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    // DELETE api/worlds/{worldId}/settlements/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int worldId, int id)
    {
        var result = await _settlementService.DeleteAsync(id);
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }
}

public record CreateSettlementRequest(string Name, int Population);
