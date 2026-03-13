using RPGEconomy.Domain.Markets;

namespace RPGEconomy.Application.Abstractions.Repositories;

public interface IMarketRepository : IRepository<Market>
{
    Task<Market?> GetBySettlementIdAsync(int settlementId);
}
