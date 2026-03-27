using RPGEconomy.Domain.Events;

namespace RPGEconomy.Application.Abstractions.Repositories;

public interface IEconomicEventRepository : IRepository<EconomicEvent>
{
    Task<IReadOnlyList<EconomicEvent>> GetBySettlementIdAsync(int settlementId);
    Task<IReadOnlyList<EconomicEvent>> GetActiveBySettlementIdAsync(int settlementId, int currentDay);
}
