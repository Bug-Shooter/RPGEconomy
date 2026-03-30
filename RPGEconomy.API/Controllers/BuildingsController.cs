using Microsoft.AspNetCore.Mvc;
using RPGEconomy.Application.Abstractions.Services;

namespace RPGEconomy.API.Controllers;

[Route("api/settlements/{settlementId}/buildings")]
[ApiController]
public class BuildingsController : ControllerBase
{
    private readonly IBuildingService _buildingService;

    public BuildingsController(IBuildingService buildingService)
        => _buildingService = buildingService;

    // GET: api/settlements/{settlementId}/buildings
    [HttpGet]
    public async Task<IActionResult> GetBySettlement(int settlementId)
    {
        var result = await _buildingService.GetBySettlementIdAsync(settlementId);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    // GET api/settlements/{settlementId}/buildings/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int settlementId, int id)
    {
        var result = await _buildingService.GetByIdAsync(id);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    // POST api/settlements/{settlementId}/buildings
    [HttpPost]
    public async Task<IActionResult> Create(
        int settlementId,
        [FromBody] CreateBuildingRequest request)
    {
        var result = await _buildingService.CreateAsync(
            settlementId, request.Name, request.RecipeId, request.WorkerCount, request.InputReserveCoverageTicks);

        if (!result.IsSuccess) return BadRequest(result.Error);
        return CreatedAtAction(nameof(GetById),
            new { settlementId, id = result.Value!.Id }, result.Value);
    }

    // PUT api/settlements/{settlementId}/buildings/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        int settlementId, int id,
        [FromBody] UpdateBuildingRequest request)
    {
        var result = await _buildingService.UpdateAsync(id, request.Name, request.WorkerCount, request.InputReserveCoverageTicks);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    // PATCH api/settlements/{settlementId}/buildings/{id}/activate
    [HttpPatch("{id}/activate")]
    public async Task<IActionResult> Activate(int settlementId, int id)
    {
        var result = await _buildingService.ActivateAsync(id);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }
    // PATCH api/settlements/{settlementId}/buildings/{id}/deactivate
    [HttpPatch("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(int settlementId, int id)
    {
        var result = await _buildingService.DeactivateAsync(id);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    // DELETE api/settlements/{settlementId}/buildings/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int settlementId, int id)
    {
        var result = await _buildingService.DeleteAsync(id);
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }
}

public record CreateBuildingRequest(string Name, int RecipeId, int WorkerCount, decimal InputReserveCoverageTicks);
public record UpdateBuildingRequest(string Name, int WorkerCount, decimal InputReserveCoverageTicks);
