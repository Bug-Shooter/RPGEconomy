using RPGEconomy.Domain.Production;

namespace RPGEconomy.Application.Abstractions.Repositories;

public interface IBuildingRepository : IRepository<Building>
{
    Task<IReadOnlyList<Building>> GetBySettlementIdAsync(int settlementId);
}
