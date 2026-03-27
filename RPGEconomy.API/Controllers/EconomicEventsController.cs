using Microsoft.AspNetCore.Mvc;
using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Application.DTOs;

namespace RPGEconomy.API.Controllers;

[Route("api/settlements/{settlementId}/economic-events")]
[ApiController]
public class EconomicEventsController : ControllerBase
{
    private readonly IEconomicEventService _economicEventService;

    public EconomicEventsController(IEconomicEventService economicEventService)
        => _economicEventService = economicEventService;

    [HttpGet]
    public async Task<IActionResult> GetBySettlement(int settlementId)
    {
        var result = await _economicEventService.GetBySettlementIdAsync(settlementId);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int settlementId, int id)
    {
        var result = await _economicEventService.GetByIdAsync(id);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost]
    public async Task<IActionResult> Create(int settlementId, [FromBody] CreateEconomicEventRequest request)
    {
        var result = await _economicEventService.CreateAsync(
            settlementId,
            request.Name,
            request.IsEnabled,
            request.StartDay,
            request.EndDay,
            request.Effects);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return CreatedAtAction(nameof(GetById), new { settlementId, id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int settlementId, int id, [FromBody] CreateEconomicEventRequest request)
    {
        var result = await _economicEventService.UpdateAsync(
            id,
            request.Name,
            request.IsEnabled,
            request.StartDay,
            request.EndDay,
            request.Effects);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("{id}/activate")]
    public async Task<IActionResult> Activate(int settlementId, int id)
    {
        var result = await _economicEventService.ActivateAsync(id);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    [HttpPost("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(int settlementId, int id)
    {
        var result = await _economicEventService.DeactivateAsync(id);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }
}

public record CreateEconomicEventRequest(
    string Name,
    bool IsEnabled,
    int StartDay,
    int? EndDay,
    IReadOnlyList<EconomicEffectDto> Effects);
