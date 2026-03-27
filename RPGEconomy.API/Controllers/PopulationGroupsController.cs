using Microsoft.AspNetCore.Mvc;
using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Application.DTOs;

namespace RPGEconomy.API.Controllers;

[Route("api/settlements/{settlementId}/population-groups")]
[ApiController]
public class PopulationGroupsController : ControllerBase
{
    private readonly IPopulationGroupService _populationGroupService;

    public PopulationGroupsController(IPopulationGroupService populationGroupService)
        => _populationGroupService = populationGroupService;

    [HttpGet]
    public async Task<IActionResult> GetBySettlement(int settlementId)
    {
        var result = await _populationGroupService.GetBySettlementIdAsync(settlementId);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int settlementId, int id)
    {
        var result = await _populationGroupService.GetByIdAsync(id);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost]
    public async Task<IActionResult> Create(int settlementId, [FromBody] CreatePopulationGroupRequest request)
    {
        var result = await _populationGroupService.CreateAsync(
            settlementId,
            request.Name,
            request.PopulationSize,
            request.ConsumptionProfile);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return CreatedAtAction(
            nameof(GetById),
            new { settlementId, id = result.Value!.Id },
            result.Value);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int settlementId, int id, [FromBody] CreatePopulationGroupRequest request)
    {
        var result = await _populationGroupService.UpdateAsync(
            id,
            request.Name,
            request.PopulationSize,
            request.ConsumptionProfile);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int settlementId, int id)
    {
        var result = await _populationGroupService.DeleteAsync(id);
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }
}

public record CreatePopulationGroupRequest(
    string Name,
    int PopulationSize,
    IReadOnlyList<ConsumptionProfileItemDto> ConsumptionProfile);
