using Microsoft.AspNetCore.Mvc;
using RPGEconomy.Application.Abstractions.Services;

namespace RPGEconomy.API.Controllers;

[Route("api/settlements/{settlementId}/market")]
[ApiController]
public class MarketsController : ControllerBase
{
    private readonly IMarketService _marketService;

    public MarketsController(IMarketService marketService)
        => _marketService = marketService;

    // GET: api/settlements/{settlementId}/market/prices>
    [HttpGet("prices")]
    public async Task<IActionResult> GetPrices(int settlementId)
    {
        var result = await _marketService.GetPricesAsync(settlementId);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpGet("products/{productTypeId}")]
    public async Task<IActionResult> GetProduct(int settlementId, int productTypeId)
    {
        var result = await _marketService.GetProductAsync(settlementId, productTypeId);
        return this.ToActionResult(result);
    }

    //// GET api/<MarketsController>/5
    //[HttpGet("{id}")]
    //public string Get(int id)
    //{
    //    return "value";
    //}

    // POST api/settlements/{settlementId}/market/products>
    [HttpPost("products")]
    public async Task<IActionResult> RegisterProduct(int settlementId, [FromBody] RegisterProductRequest request)
    {
        var result = await _marketService.RegisterProductAsync(
            settlementId,
            request.ProductTypeId,
            request.InitialPrice);

        return this.ToActionResult(result);
    }

    [HttpPut("products/{productTypeId}")]
    public async Task<IActionResult> UpdateProductState(
        int settlementId,
        int productTypeId,
        [FromBody] UpdateMarketProductRequest request)
    {
        var result = await _marketService.UpdateProductStateAsync(
            settlementId,
            productTypeId,
            request.Supply,
            request.Demand);

        return this.ToActionResult(result);
    }

    //// PUT api/<MarketsController>/5
    //[HttpPut("{id}")]
    //public void Put(int id, [FromBody] string value)
    //{
    //}

    //// DELETE api/<MarketsController>/5
    //[HttpDelete("{id}")]
    //public void Delete(int id)
    //{
    //}
}

public record RegisterProductRequest(int ProductTypeId, decimal InitialPrice);
public record UpdateMarketProductRequest(decimal Supply, decimal Demand);

static class MarketsControllerResultExtensions
{
    public static IActionResult ToActionResult(this ControllerBase controller, RPGEconomy.Domain.Common.Result result)
    {
        if (result.IsSuccess)
            return controller.Ok();

        return IsNotFound(result.Error) ? controller.NotFound(result.Error) : controller.BadRequest(result.Error);
    }

    public static IActionResult ToActionResult<T>(this ControllerBase controller, RPGEconomy.Domain.Common.Result<T> result)
    {
        if (result.IsSuccess)
            return controller.Ok(result.Value);

        return IsNotFound(result.Error) ? controller.NotFound(result.Error) : controller.BadRequest(result.Error);
    }

    private static bool IsNotFound(string? error) =>
        !string.IsNullOrWhiteSpace(error) && error.Contains("не найден", StringComparison.OrdinalIgnoreCase);
}
