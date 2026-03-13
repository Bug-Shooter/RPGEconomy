using Microsoft.AspNetCore.Mvc;
using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Application.DTOs;

namespace RPGEconomy.API.Controllers;

[Route("api/recipes")]
[ApiController]
public class RecipesController : ControllerBase
{
    private readonly IRecipeService _recipeService;

    public RecipesController(IRecipeService recipeService)
        => _recipeService = recipeService;

    // GET: api/recipes
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _recipeService.GetAllAsync();
        return Ok(result.Value);
    }

    // GET api/recipes/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _recipeService.GetByIdAsync(id);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    // POST api/recipes
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRecipeRequest request)
    {
        var result = await _recipeService.CreateAsync(
            request.Name, request.LaborDaysRequired, request.Inputs, request.Outputs);

        if (!result.IsSuccess) return BadRequest(result.Error);
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    // PUT api/recipes/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateRecipeRequest request)
    {
        var result = await _recipeService.UpdateAsync(
            id, request.Name, request.LaborDaysRequired, request.Inputs, request.Outputs);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    // DELETE api/<recipes/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _recipeService.DeleteAsync(id);
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }
}

public record CreateRecipeRequest(
    string Name,
    double LaborDaysRequired,
    IReadOnlyList<RecipeIngredientDto> Inputs,
    IReadOnlyList<RecipeIngredientDto> Outputs);
