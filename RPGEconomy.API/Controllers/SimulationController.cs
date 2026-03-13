using Microsoft.AspNetCore.Mvc;
using RPGEconomy.Application.Abstractions.Services;

namespace RPGEconomy.API.Controllers;

[Route("api/worlds/{worldId}/simulation")]
[ApiController]
public class SimulationController : ControllerBase
{
    private readonly ISimulationEngine _simulationEngine;

    public SimulationController(ISimulationEngine simulationEngine)
        => _simulationEngine = simulationEngine;

    //// GET: api/<SimulationController>
    //[HttpGet]
    //public IEnumerable<string> Get()
    //{
    //    return new string[] { "value1", "value2" };
    //}

    //// GET api/<SimulationController>/5
    //[HttpGet("{id}")]
    //public string Get(int id)
    //{
    //    return "value";
    //}

    // POST api/worlds/{worldId}/simulation/advance
    [HttpPost("advance")]
    public async Task<IActionResult> Advance(
            int worldId,
            [FromBody] AdvanceTimeRequest request)
    {
        if (request.Days <= 0)
            return BadRequest("Количество дней должно быть больше нуля");

        var result = await _simulationEngine.AdvanceAsync(worldId, request.Days);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    //// PUT api/<SimulationController>/5
    //[HttpPut("{id}")]
    //public void Put(int id, [FromBody] string value)
    //{
    //}

    //// DELETE api/<SimulationController>/5
    //[HttpDelete("{id}")]
    //public void Delete(int id)
    //{
    //}
}

public record AdvanceTimeRequest(int Days);
