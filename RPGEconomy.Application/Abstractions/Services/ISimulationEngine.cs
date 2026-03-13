using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;

namespace RPGEconomy.Application.Abstractions.Services;

public interface ISimulationEngine
{
    Task<Result<SimulationResultDto>> AdvanceAsync(int worldId, int days);
}
