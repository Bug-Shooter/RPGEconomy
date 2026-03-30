using Microsoft.AspNetCore.Mvc;
using RPGEconomy.Application.Abstractions.Services;

namespace RPGEconomy.API.Controllers;

[Route("api/settlements/{settlementId}/warehouse")]
[ApiController]
public class WarehousesController : ControllerBase
{
    private readonly IWarehouseService _warehouseService;

    public WarehousesController(IWarehouseService warehouseService)
        => _warehouseService = warehouseService;

    [HttpGet]
    public async Task<IActionResult> GetBySettlement(int settlementId)
    {
        var result = await _warehouseService.GetBySettlementIdAsync(settlementId);
        return this.ToActionResult(result);
    }

    [HttpPut("items")]
    public async Task<IActionResult> SetStockItem(int settlementId, [FromBody] SetWarehouseStockRequest request)
    {
        var result = await _warehouseService.SetStockItemAsync(
            settlementId,
            request.ProductTypeId,
            request.Quantity,
            request.Quality);

        return this.ToActionResult(result);
    }
}

public record SetWarehouseStockRequest(int ProductTypeId, decimal Quantity, string? Quality);
