using Microsoft.AspNetCore.Mvc;
using RPGEconomy.Application.Abstractions.Services;

namespace RPGEconomy.API.Controllers;

[Route("api/currencies")]
[ApiController]
public class CurrenciesController : ControllerBase
{
    private readonly ICurrencyService _currencyService;

    public CurrenciesController(ICurrencyService currencyService)
        => _currencyService = currencyService;


    // GET: api/currencies
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _currencyService.GetAllAsync();
        return Ok(result.Value);
    }

    // GET api/currencies/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _currencyService.GetByIdAsync(id);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    // POST api/currencies
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCurrencyRequest request)
    {
        var result = await _currencyService.CreateAsync(
            request.Name, request.Code, request.ExchangeRateToBase);

        if (!result.IsSuccess) return BadRequest(result.Error);
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    // PUT api/currencies/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateCurrencyRequest request)
    {
        var result = await _currencyService.UpdateAsync(
            id, request.Name, request.Code, request.ExchangeRateToBase);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    // DELETE api/currencies/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _currencyService.DeleteAsync(id);
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }
}

public record CreateCurrencyRequest(string Name, string Code, decimal ExchangeRateToBase);
