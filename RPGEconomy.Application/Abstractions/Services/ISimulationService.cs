using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;

namespace RPGEconomy.Application.Abstractions.Services;

public interface ISimulationService
{
    Task<Result<SimulationResultDto>> AdvanceAsync(
        RunSimulationCommand command,
        CancellationToken cancellationToken = default);
}
