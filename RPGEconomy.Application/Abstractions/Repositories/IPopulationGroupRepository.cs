using RPGEconomy.Domain.Population;

namespace RPGEconomy.Application.Abstractions.Repositories;

public interface IPopulationGroupRepository : IRepository<PopulationGroup>
{
    Task<IReadOnlyList<PopulationGroup>> GetBySettlementIdAsync(int settlementId);
}
