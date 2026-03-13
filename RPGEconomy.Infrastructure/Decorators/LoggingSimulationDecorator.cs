using Microsoft.Extensions.Logging;
using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;

namespace RPGEconomy.Infrastructure.Decorators;

public class LoggingSimulationDecorator : ISimulationEngine
{
    private readonly ISimulationEngine _inner;
    private readonly ILogger<LoggingSimulationDecorator> _logger;

    public LoggingSimulationDecorator(
        ISimulationEngine inner,
        ILogger<LoggingSimulationDecorator> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<Result<SimulationResultDto>> AdvanceAsync(int worldId, int days)
    {
        _logger.LogInformation(
            "Simulation started. WorldId={WorldId}, Days={Days}", worldId, days);

        var result = await _inner.AdvanceAsync(worldId, days);

        if (result.IsSuccess)
            _logger.LogInformation(
                "Simulation completed. WorldId={WorldId}, Day {Before}→{After}",
                worldId,
                result.Value!.DaysBefore,
                result.Value!.DaysAfter);
        else
            _logger.LogWarning(
                "Simulation failed. WorldId={WorldId}, Error={Error}",
                worldId, result.Error);

        return result;
    }
}

