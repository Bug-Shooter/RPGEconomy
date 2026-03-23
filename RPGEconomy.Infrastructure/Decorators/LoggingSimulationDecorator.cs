using Microsoft.Extensions.Logging;
using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;

namespace RPGEconomy.Infrastructure.Decorators;

public class LoggingSimulationDecorator : ISimulationExecutor
{
    private readonly ISimulationExecutor _inner;
    private readonly ILogger<LoggingSimulationDecorator> _logger;

    public LoggingSimulationDecorator(
        ISimulationExecutor inner,
        ILogger<LoggingSimulationDecorator> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<Result<SimulationExecutionResult>> ExecuteAsync(
        SimulationExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Simulation started. JobId={JobId}, WorldId={WorldId}, Days={Days}",
            request.JobId,
            request.WorldId,
            request.Days);

        var result = await _inner.ExecuteAsync(request, cancellationToken);

        if (result.IsSuccess)
            _logger.LogInformation(
                "Simulation completed. JobId={JobId}, WorldId={WorldId}, Day {Before}->{After}",
                request.JobId,
                request.WorldId,
                result.Value!.Result.DaysBefore,
                result.Value!.Result.DaysAfter);
        else
            _logger.LogWarning(
                "Simulation failed. JobId={JobId}, WorldId={WorldId}, Error={Error}",
                request.JobId,
                request.WorldId,
                result.Error);

        return result;
    }
}

