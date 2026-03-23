using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;

namespace RPGEconomy.Application.Abstractions.Services;

public interface ISimulationExecutor
{
    Task<Result<SimulationExecutionResult>> ExecuteAsync(
        SimulationExecutionRequest request,
        CancellationToken cancellationToken = default);
}
